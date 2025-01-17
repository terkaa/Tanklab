using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using BestHTTP;
using BestHTTP.Extensions;
using BestHTTP.PlatformSupport.Memory;
using static BestHTTP.HTTPManager;

using BestMQTT.Packets;

namespace BestMQTT.Transports
{
    internal sealed class RawTCPTransport : Transport, IHeartbeat
    {
        private ConcurrentQueue<Packet> incomingPackets = new ConcurrentQueue<Packet>();
        
        private RawTCPReceiver receiver = null;
        private RawTCPSender sender = null;

        public RawTCPTransport(MQTTClient parent)
            :base(parent)
        {
        }

        internal override void BeginConnect(CancellationToken token = default)
        {
            base.BeginConnect(token);

            if (token.IsCancellationRequested)
                return;

            if (this.State != TransportStates.Initial)
                throw new Exception($"{nameof(RawTCPTransport)} couldn't {nameof(BeginConnect)} as it's already in state {this.State}!");

            Logger.Information(nameof(RawTCPTransport), $"{nameof(BeginConnect)}({BestHTTP.JSON.LitJson.JsonMapper.ToJson(this.Parent.Options)})", this.Context);

            var result = Dns.BeginGetHostAddresses(this.Parent.Options.Host, OnGetHostAddressesFinished, null);

            Heartbeats.Subscribe(this);
        }

        internal override void Send(BufferSegment buffer)
        {
            if (this.State != TransportStates.Connected)
            {
                Logger.Warning(nameof(RawTCPTransport), $"Send called while it's not in the Connected state! State: {this.State}", this.Context);
                return;
            }

            this.sender.Send(buffer);
        }

        private void OnGetHostAddressesFinished(IAsyncResult ar)
        {
            try
            {
                IPAddress[] addresses = Dns.EndGetHostAddresses(ar);

                if (base.ConnectCancellationToken.IsCancellationRequested)
                {
                    Logger.Information(nameof(RawTCPTransport), $"{nameof(OnGetHostAddressesFinished)} - IsCancellationRequested", this.Context);
                    this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.Disconnected));
                    return;
                }

                Logger.Information(nameof(RawTCPTransport), $"{nameof(OnGetHostAddressesFinished)} Received {addresses?.Length} IP Addresses", this.Context);

                var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //s.NoDelay = true;
                s.BeginConnect(addresses, this.Parent.Options.Port, OnConnectFinished, s);
            }
            catch (Exception ex)
            {
                Logger.Exception(nameof(RawTCPTransport), nameof(OnGetHostAddressesFinished), ex, this.Context);
                this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.Disconnected));
            }
        }

        private void OnConnectFinished(IAsyncResult ar)
        {
            Socket s = ar.AsyncState as Socket;
            try
            {
                s.EndConnect(ar);

                if (base.ConnectCancellationToken.IsCancellationRequested)
                {
                    Logger.Information(nameof(RawTCPTransport), $"{nameof(OnConnectFinished)} - IsCancellationRequested", this.Context);
                    this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.Disconnected));
                    return;
                }

                Logger.Information(nameof(RawTCPTransport), $"{nameof(OnConnectFinished)}", this.Context);

                // Start to receive
                this.receiver = new RawTCPReceiver(this, s, 256, this.incomingPackets);
                this.receiver.StartReceive();

                // And start sending
                this.sender = new RawTCPSender(this, s, 256);

                //this.State = TransportStates.Connected;
                //this.Parent.OnTransportConnected();
                this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.Connected));
            }
            catch(Exception ex)
            {
                Logger.Exception(nameof(RawTCPTransport), nameof(OnConnectFinished), ex, this.Context);
                this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.Disconnected));
            }
        }

        internal override void BeginDisconnect()
        {
            if (this.State >= TransportStates.Disconnecting)
                return;

            ChangeStateTo(TransportStates.Disconnecting, string.Empty);
            try
            {
                this.sender.Socket.Shutdown(SocketShutdown.Send);
                this.sender.Socket.BeginDisconnect(false, OnDisconnectFinished, this.sender.Socket);
            }
            catch//(Exception ex)
            {
                //Logger.Exception(nameof(TCPTransport), nameof(BeginDisconnect), ex, this.Context);
                ChangeStateTo(TransportStates.Disconnected, "");
            }
        }

        private void OnDisconnectFinished(IAsyncResult ar)
        {
            Socket socket = ar.AsyncState as Socket;
            try
            {
                socket.EndDisconnect(ar);
            }
            catch//(Exception ex)
            {
                //Logger.Exception(nameof(TCPTransport), nameof(OnDisconnectFinished), ex, this.Context);
            }
            finally
            {
                this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.Disconnected));
            }
        }

        internal void EnqueueTransportEvent(TransportEvent @event)
        {
            this.transportEvents.Enqueue(@event);
        }

        void IHeartbeat.OnHeartbeatUpdate(TimeSpan dif)
        {
            while (this.transportEvents.TryDequeue(out var result))
            {
                switch (result.Type)
                {
                    case TransportEventTypes.StateChange:
                        ChangeStateTo(result.ToState, reason: "");
                        break;

                    case TransportEventTypes.MQTTException:
                        this.Parent.MQTTError(result.Source, result.Exception);
                        break;
                }
            }
        }

        protected override void CleanupAfterDisconnect()
        {
            base.CleanupAfterDisconnect();

            Heartbeats.Unsubscribe(this);
        }
    }
}
