using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using BestHTTP;
using BestHTTP.Extensions;
using BestHTTP.Logger;

using static BestHTTP.HTTPManager;

using BestMQTT.Packets;
using BestMQTT.Packets.Builders;
using BestMQTT.Transports;

namespace BestMQTT
{
    public delegate void OnConnectedDelegate(MQTTClient client);
    public delegate void OnServerConnectAckMessageDelegate(MQTTClient client, ServerConnectAckMessage message);
    public delegate void OnApplicationMessageDelegate(MQTTClient client, ApplicationMessage message);
    public delegate void OnAuthenticationMessageDelegate(MQTTClient client, AuthenticationMessage message);
    public delegate void OnErrorDelegate(MQTTClient client, string error);
    public delegate void OnDisconnectDelegate(MQTTClient client, DisconnectReasonCodes reasonCode, string reasonMessage);
    public delegate void OnStateChangedDelegate(MQTTClient client, ClientStates oldState, ClientStates newState);
    public delegate ConnectPacketBuilder ConnectPacketBuilderDelegate(MQTTClient client, ConnectPacketBuilder builder);

    public sealed partial class MQTTClient : IHeartbeat
    {
        /// <summary>
        /// Connection related options.
        /// </summary>
        public ConnectionOptions Options { get; private set; }

        /// <summary>
        /// Called when the client successfully connected to the broker.
        /// </summary>
        public event OnConnectedDelegate OnConnected;

        /// <summary>
        /// Called when the broker acknowledged the client's connect packet.
        /// </summary>
        public event OnServerConnectAckMessageDelegate OnServerConnectAckMessage;

        /// <summary>
        /// Called for every application message sent by the broker.
        /// </summary>
        public event OnApplicationMessageDelegate OnApplicationMessage;

        /// <summary>
        /// Called when an authentication packet is received from the broker as part of the extended authentication process.
        /// </summary>
        public event OnAuthenticationMessageDelegate OnAuthenticationMessage;

        /// <summary>
        /// Called when an unexpected, unrecoverable error happens. After this event an OnDisconnect event is called too.
        /// </summary>
        public event OnErrorDelegate OnError;

        /// <summary>
        /// Called after the client disconnects from the broker.
        /// </summary>
        public event OnDisconnectDelegate OnDisconnect;

        /// <summary>
        /// Called for every internal state change of the client.
        /// </summary>
        public event OnStateChangedDelegate OnStateChanged;

        /// <summary>
        /// Current state of the client. State changed events are emitted through the OnStateChanged event.
        /// </summary>
        public ClientStates State { get => this._state;
            private set
            {
                var oldState = this._state;
                if (oldState != value)
                {
                    this._state = value;

                    try
                    {
                        this.OnStateChanged?.Invoke(this, oldState, this._state);
                    }
                    catch (MQTTException ex)
                    {
                        this.MQTTError(nameof(OnStateChanged), ex);
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(nameof(MQTTClient), nameof(OnStateChanged), ex, this.Context);
                    }
                }
            }
        }
        private ClientStates _state;

        /// <summary>
        /// Options negotiated with the broker.
        /// </summary>
        public NegotiatedOptions NegotiatedOptions { get; private set; }

        /// <summary>
        /// Session instance to persist QoS data.
        /// </summary>
        public Session Session { get; internal set; }

        /// <summary>
        /// Context of the MQTTClient and all child instances (like its transport, etc.) that can produce log outputs.
        /// </summary>
        public LoggingContext Context { get; private set; }

        internal UInt32 GetNextSubscriptionID() => (UInt32)Interlocked.Increment(ref this._lastSubscriptionID);
        private long _lastSubscriptionID = 0;

        internal UInt16 GetNextPacketID()
        {
            var id = this._lastPacketID++;
            id %= UInt16.MaxValue;

            if (id == 0)
                id++;

            // when restarted it might generate packets in use in unacknowledged packets!
            if (this.Session.UnacknowledgedPackets.IsPacketIDInUse((UInt16)id) || this.Session.PublishReleasedPacketIDs.Contains((UInt16)id))
                id = GetNextPacketID();

            return (UInt16)id;
        }
        private long _lastPacketID = 0;

        private Transport transport;
        private DateTime lastPacketSentAt;
        private DateTime pingSentAt = DateTime.MinValue;

        //private ConnectPacketBuilder connectPacketBuilder;
        
        private Dictionary<UInt16, string> _serverTopicAliasMapping;

        private ConcurrentDictionary<UInt16, Subscription> pendingSubscriptions = new ConcurrentDictionary<UInt16, Subscription>();
        private ConcurrentDictionary<UInt32, Subscription> subscriptions = new ConcurrentDictionary<UInt32, Subscription>();

        private ConcurrentDictionary<UInt16, List<UnsubscribeTopicFilter>> pendingUnsubscriptions = new ConcurrentDictionary<UInt16, List<UnsubscribeTopicFilter>>();

        private UInt16 _sendQuota;
        private UInt16 _maxQuota;

        private ConcurrentQueue<Packet> _outgoingPackets = new ConcurrentQueue<Packet>();
        private volatile int _bufferPackets;

        public MQTTClient(ConnectionOptions options)
        {
            this.Context = new LoggingContext(this);

            this.Options = options;
            this.NegotiatedOptions = new NegotiatedOptions();

            HTTPManager.Setup();
            Heartbeats.Subscribe(this);
        }

        private void Send(in Packet packet)
        {
            Logger.Information(nameof(MQTTClient), $"{nameof(Send)}({packet.ToString()})", this.Context);

            this._outgoingPackets.Enqueue(packet);

            // if buffering is off, prepare and send packets immediately.
            if (this._bufferPackets == 0)
                EndPacketBuffer();
        }

        internal void TransportConnected()
        {
            if (this.State >= ClientStates.Disconnecting)
                return;

            Logger.Information(nameof(MQTTClient), nameof(TransportConnected), this.Context);

            var packetBuilder = this.CreateConnectPacketBuilder();

            if (this.connectBag != null && this.connectBag.connectPacketBuilderFactory != null)
            {
                try
                {
                    packetBuilder = this.connectBag.connectPacketBuilderFactory(this, packetBuilder);
                }
                catch (Exception ex)
                {
                    Logger.Exception(nameof(MQTTClient), nameof(this.connectBag.connectPacketBuilderFactory), ex, this.Context);
                }
            }
            else
                Logger.Warning(nameof(MQTTClient), $"Not ConnectPacketBuilder function! Connecting with default packet...", this.Context);

            var packetBuilderResult = packetBuilder.Build();

            this.Session = packetBuilderResult.session;
            this.NegotiatedOptions.ClientKeepAlive = packetBuilderResult.clientKeepAlive;
            this.NegotiatedOptions.ClientMaximumPacketSize = packetBuilderResult.clientMaximumPacketSize;
            this.NegotiatedOptions.ClientReceiveMaximum = packetBuilderResult.clientReceiveMaximum;

            this.Send(in packetBuilderResult.packet);

            this.State = ClientStates.TransportConnected;
        }

        internal void TransportDisconnectedWithError(string reason)
        {
            if (this.State >= ClientStates.Disconnected)
                return;

            Logger.Information(nameof(MQTTClient), $"{nameof(TransportDisconnectedWithError)}(\"{reason}\")", this.Context);

            Error("Transport", DisconnectReasonCodes.UnspecifiedError, reason);
        }

        internal void TransportDisconnected(string reason)
        {
            if (this.State >= ClientStates.Disconnected)
                return;

            Logger.Information(nameof(MQTTClient), $"{nameof(TransportDisconnected)}(\"{reason}\")", this.Context);

            //Error("Transport", reason);
            SetDisconnected(DisconnectReasonCodes.NormalDisconnection, reason);
        }

        internal void AddSubscription(UInt16 packetId, Subscription subscription)
        {
            this.pendingSubscriptions.TryAdd(packetId, subscription);
            this.subscriptions.TryAdd(subscription.ID, subscription);
        }

        internal void AddUnsubscription(UInt16 packetId, List<UnsubscribeTopicFilter> filter)
        {
            if (this.State != ClientStates.Connected)
                throw new Exception($"Not connected! Current state: {this.State}");

            this.pendingUnsubscriptions.TryAdd(packetId, filter);
        }

        private void SendPublishPacket(UInt16 packetId, in Packet publishPacket)
        {
            if (publishPacket.Type != PacketTypes.Publish)
                throw new ArgumentException($"{nameof(SendPublishPacket)} expected a PUBLISH packet, received {publishPacket.Type}");

            Logger.Verbose(nameof(MQTTClient), $"{nameof(SendPublishPacket)}({packetId}) SendQuota: {this._sendQuota}", this.Context);

            if (packetId != 0)
            {
                this.Session.UnacknowledgedPackets.Add(packetId, in publishPacket);
                this._sendQuota--;
            }

            this.Send(in publishPacket);
        }

        internal void MQTTError(string source, MQTTException exception)
        {
            this.MQTTError(source, exception.MQTTError, exception.Message);
        }

        internal void MQTTError(string source, MQTTErrorTypes errorType, string reason)
        {
            Logger.Error(nameof(MQTTClient), $"MQTTError(\"{source}\", {errorType}, \"{reason}\")", this.Context);

            DisconnectReasonCodes disconnectReason;

            switch (errorType)
            {
                case MQTTErrorTypes.MalformedPacket: disconnectReason = DisconnectReasonCodes.MalformedPacket; break;
                case MQTTErrorTypes.ProtocolError: disconnectReason = DisconnectReasonCodes.ProtocolError; break;
                case MQTTErrorTypes.PacketTooLarge: disconnectReason = DisconnectReasonCodes.PacketTooLarge; break;
                case MQTTErrorTypes.ReceiveMaximumExceeded: disconnectReason = DisconnectReasonCodes.ReceiveMaximumExceeded; break;
                default:
                    throw new NotImplementedException($"Unknown internal MQTT error type({errorType}) with reason \"{reason}\"");
            }

            var builder = new DisconnectPacketBuilder(this);
            if (this.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                builder.WithReasonCode(disconnectReason);
            this.BeginDisconnect(builder);

            this.Error(source, disconnectReason, reason);
        }

        private void Error(string source, DisconnectReasonCodes code, string reason)
        {
            reason = reason ?? string.Empty;

            Logger.Information(nameof(MQTTClient), $"{nameof(Error)}(\"{source}\", {code}, \"{reason}\"", this.Context);

            try
            {
                if (this.connectBag != null)
                    this.connectBag.errorReason = reason;

                this.OnError?.Invoke(this, reason);
            }
            catch (Exception ex)
            {
                Logger.Exception(nameof(MQTTClient), $"{nameof(OnError)}(\"{source}\", {code}, \"{reason}\")", ex, this.Context);
            }

            this.transport?.BeginDisconnect();
            //this.State = ClientStates.Disconnected;
            SetDisconnected(code, reason);
        }

        private void SetDisconnected(DisconnectReasonCodes code, string reason)
        {
            if (this.State >= ClientStates.Disconnected)
                return;

            Logger.Information(nameof(MQTTClient), $"{nameof(SetDisconnected)}({code}, \"{reason}\"", this.Context);

            this.State = ClientStates.Disconnected;

            try
            {
                this.OnDisconnect?.Invoke(this, code, reason);
            }
            catch(Exception ex)
            {
                Logger.Exception(nameof(MQTTClient), $"{nameof(OnDisconnect)}(\"{code}\", \"{reason}\")", ex, this.Context);
            }
        }

        void IHeartbeat.OnHeartbeatUpdate(TimeSpan dif)
        {
            this.BeginPacketBuffer();
            try
            {
                while (this.transport.IncomingPackets.TryDequeue(out var packet))
                {
                    if (this.State >= ClientStates.Disconnecting)
                        continue;

                    Logger.Information(nameof(MQTTClient), $"Processing Incoming Packet '{packet.Type}'", this.Context);

                    try
                    {
                        switch (packet.Type)
                        {
                            case PacketTypes.ConnectAck: HandleConnectAckPacket(packet); break;
                            case PacketTypes.Disconnect: HandleDisconnectPacket(packet); break;

                            case PacketTypes.SubscribeAck: HandleSubscribeAckPacket(packet); break;
                            case PacketTypes.UnsubscribeAck: HandleUnsubscribeAckPacket(packet); break;

                            case PacketTypes.Publish: HandlePublishPacket(packet); break;

                            // QoS 2, delivery part 1
                            case PacketTypes.PublishReceived: HandlePublishReceivedPacket(packet); break;
                            // QoS 2, delivery part 2
                            case PacketTypes.PublishRelease: HandlePublishReleasePacket(packet); break;
                            // QoS 2, delivery part 3
                            case PacketTypes.PublishComplete: HandlePublishCompletePacket(packet); break;

                            // QoS 1:
                            case PacketTypes.PublishAck: HandlePublishAckPacket(packet); break;

                            case PacketTypes.PingResponse:
                                Logger.Verbose(nameof(MQTTClient), $"Received Ping Response!", this.Context);
                                this.pingSentAt = DateTime.MinValue;
                                break;

                            case PacketTypes.Auth: HandleAuthPacket(packet); break;

                            default: Logger.Warning(nameof(MQTTClient), $"Unhandled incoming packet '{packet.Type}'!", this.Context); break;
                        }
                    }
                    catch(MQTTException ex)
                    {
                        Logger.Exception(nameof(MQTTClient), $"{packet.Type}", ex, this.Context);
                        this.MQTTError($"OnHeartbeatUpdate.IncomingPackets({packet.Type})", ex);
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(nameof(MQTTClient), $"{packet.Type}", ex, this.Context);
                    }
                }

                DateTime now = DateTime.Now;

                switch(this.State)
                {
                    case ClientStates.TransportConnecting:
                    case ClientStates.TransportConnected:
                        if (this.transport.ConnectCancellationToken.IsCancellationRequested)
                            this.CreateDisconnectPacketBuilder()
                                .WithReasonCode(DisconnectReasonCodes.MaximumConnectTime)
                                .BeginDisconnect();
                        break;

                    case ClientStates.Connected:
                        var keepAlive = this.NegotiatedOptions.ServerOptions.ServerKeepAlive ?? this.NegotiatedOptions.ClientKeepAlive;
                        if (keepAlive > 0)
                        {
                            if (this.pingSentAt == DateTime.MinValue && now - lastPacketSentAt >= TimeSpan.FromSeconds(keepAlive))
                            {
                                var pingPacket = new Packet { Type = PacketTypes.PingRequest };
                                this.Send(in pingPacket);
                                this.pingSentAt = now;

                                Logger.Verbose(nameof(MQTTClient), $"Sent Ping Request ({now.ToLongTimeString()})", this.Context);
                            }

                            var diff = now - this.pingSentAt;
                            TimeSpan max = TimeSpan.FromSeconds(Math.Max(keepAlive / 2, 1));
                            if (this.pingSentAt != DateTime.MinValue && diff >= max)
                            {
                                Logger.Verbose(nameof(MQTTClient), $"Not received Ping Response in the given time! diff: {diff}, max: {max}", this.Context);
                                Error(nameof(MQTTClient), DisconnectReasonCodes.KeepAliveTimeout, "Not received Ping Response in a reasonable time!");
                            }
                        }
                        break;

                      case ClientStates.Disconnected:
                        Heartbeats.Unsubscribe(this);

                        if (this.transport.ConnectCancellationToken.IsCancellationRequested)
                            this.connectBag?.completionSource?.TrySetCanceled();
                        else
                            this.connectBag?.completionSource?.TrySetException(new Exception(this.connectBag?.errorReason));
                        this.connectBag = null;
                        break;
                }
            }
            finally
            {
                this.EndPacketBuffer();
            }
        }
    }
}
