using System;
using System.Collections.Concurrent;
using System.Threading;

using BestHTTP;
using BestHTTP.Extensions;
using BestHTTP.Logger;
using BestHTTP.PlatformSupport.Memory;

using BestMQTT.Packets;
using BestMQTT.Packets.Readers;

using static BestHTTP.HTTPManager;

namespace BestMQTT.Transports
{
    public enum TransportStates
    {
        Initial,
        Connecting,
        Connected,
        Disconnecting,
        Disconnected,
        DisconnectedWithError
    }

    public enum TransportEventTypes
    {
        StateChange,
        MQTTException
    }

    public readonly struct TransportEvent
    {
        public readonly TransportEventTypes Type;

        public readonly TransportStates ToState;
        public readonly string Reason;

        public readonly string Source;
        public readonly MQTTException Exception;

        public TransportEvent(TransportEventTypes type, TransportStates toState)
            :this(type, toState, null)
        {
        }

        public TransportEvent(TransportEventTypes type, TransportStates toState, string reason)
        {
            this.Type = type;
            this.ToState = toState;
            this.Reason = reason;

            this.Source = null;
            this.Exception = null;
        }

        public TransportEvent(TransportEventTypes type, MQTTException exception, string source)
        {
            this.Type = type;
            this.ToState = TransportStates.Initial;
            this.Reason = null;

            this.Source = source;
            this.Exception = exception;
        }
    }

    public abstract class Transport : IHeartbeat
    {
        /// <summary>
        /// State of the transport.
        /// </summary>
        public TransportStates State { get; private set; } = TransportStates.Initial;

        /// <summary>
        /// Parent MQTTClient instance that the transport is created for.
        /// </summary>
        public MQTTClient Parent { get; private set; }

        /// <summary>
        /// Received & parsed packets, sent by the server.
        /// </summary>
        public ConcurrentQueue<Packet> IncomingPackets { get; private set; } = new ConcurrentQueue<Packet>();

        public LoggingContext Context { get; private set; }

        /// <summary>
        /// Intermediate stream holding incomplete packet bytes.
        /// </summary>
        internal PeekableIncomingSegmentStream ReceiveStream { get; private set; } = new PeekableIncomingSegmentStream();

        public CancellationToken ConnectCancellationToken { get; protected set; } = default;

        /// <summary>
        /// Transport event queue generated on receive/send threads that must be processed on the main thread.
        /// </summary>
        protected ConcurrentQueue<TransportEvent> transportEvents = new ConcurrentQueue<TransportEvent>();

        protected Transport(MQTTClient parent)
        {
            this.Parent = parent;
            this.Context = new LoggingContext(this);
            this.Context.Add("Parent", this.Parent.Context);
        }

        internal virtual void BeginConnect(CancellationToken token)
        {
            this.ConnectCancellationToken = token;
            Heartbeats.Subscribe(this);
        }

        internal abstract void Send(BufferSegment buffer);
        internal abstract void BeginDisconnect();

        internal void TryParseIncomingPackets()
        {
            Logger.Information(nameof(Transport), $"{nameof(TryParseIncomingPackets)} available: {this.ReceiveStream.Length}", this.Context);

            var (success, packet) = IncomingPacketReader.TryToReadFrom(this);

            while (success)
            {
                Logger.Information(nameof(Transport), $"{nameof(TryParseIncomingPackets)}: Parsed packet {packet.Type}, available: {this.ReceiveStream.Length}", this.Context);

                this.IncomingPackets.Enqueue(packet);

                (success, packet) = IncomingPacketReader.TryToReadFrom(this);
            }
        }

        protected void ChangeStateTo(TransportStates newState, string reason)
        {
            Logger.Information(nameof(Transport), $"{nameof(ChangeStateTo)}({this.State} => {newState}, \"{reason}\")", this.Context);

            if (this.State == newState)
                return;

            var oldState = this.State;
            this.State = newState;

            switch (newState)
            {
                case TransportStates.Connected:
                    if (!this.ConnectCancellationToken.IsCancellationRequested)
                        this.Parent.TransportConnected();
                    break;
                case TransportStates.DisconnectedWithError:
                    if (oldState == TransportStates.Disconnected)
                        break;
                    this.Parent.TransportDisconnectedWithError(reason);
                    this.CleanupAfterDisconnect();
                    break;

                case TransportStates.Disconnected:
                    if (oldState == TransportStates.DisconnectedWithError)
                        break;
                    this.Parent.TransportDisconnected(reason);
                    this.CleanupAfterDisconnect();
                    break;
            }
        }

        public void OnHeartbeatUpdate(TimeSpan dif)
        {
            while (this.transportEvents.TryDequeue(out var result))
            {
                switch (result.Type)
                {
                    case TransportEventTypes.StateChange:
                        ChangeStateTo(result.ToState, reason: result.Reason);
                        break;

                    case TransportEventTypes.MQTTException:
                        this.Parent.MQTTError(result.Source, result.Exception);
                        break;
                }
            }
        }

        protected virtual void CleanupAfterDisconnect()
        {
            Logger.Information(nameof(Transport), $"{nameof(CleanupAfterDisconnect)}", this.Context);

            this.ReceiveStream?.Dispose();
            this.ReceiveStream = null;

            Heartbeats.Unsubscribe(this);
        }
    }
}
