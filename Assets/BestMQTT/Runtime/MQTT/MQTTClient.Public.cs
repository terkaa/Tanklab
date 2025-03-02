using System;
using System.Threading;

using BestHTTP.Extensions;
using BestHTTP.PlatformSupport.Memory;
using static BestHTTP.HTTPManager;

using BestMQTT.Packets;
using BestMQTT.Packets.Builders;
using BestMQTT.Transports;
using BestMQTT.Packets.Utils;
using System.Threading.Tasks;

namespace BestMQTT
{
    /// <summary>
    /// Possible states of the MQTTClient.
    /// </summary>
    public enum ClientStates
    {
        /// <summary>
        /// State right after constructing the MQTTClient.
        /// </summary>
        Initial,

        /// <summary>
        /// Connection process initiated.
        /// </summary>
        TransportConnecting,

        /// <summary>
        /// Transport successfully connected to the broker.
        /// </summary>
        TransportConnected,

        /// <summary>
        /// Connect packet sent and acknowledgement received.
        /// </summary>
        Connected,

        /// <summary>
        /// Disconnect process initiated.
        /// </summary>
        Disconnecting,

        /// <summary>
        /// Client disconnected from the broker. This could be the result either of a graceful termination or an unexpected error.
        /// </summary>
        Disconnected
    }

    public sealed partial class MQTTClient
    {
        private ConnectBag connectBag;

        /// <summary>
        /// With the use of BeginPacketBuffer and EndPacketBuffer sent messages can be buffered and sent in less network packets. It supports nested Begin-EndPacketBuffer calls.
        /// </summary>
        public void BeginPacketBuffer()
        {
            Interlocked.Increment(ref this._bufferPackets);
        }

        /// <summary>
        /// Call this after a BeginPacketBuffer.
        /// </summary>
        public void EndPacketBuffer()
        {
            if (this._bufferPackets == 0 || Interlocked.Decrement(ref this._bufferPackets) == 0)
            {
                byte[] initialBuffer = null;
                try
                {
                    if (this._outgoingPackets.Count > 0)
                    {
                        initialBuffer = BufferPool.Get(256, true);
                        using (var ms = new BufferPoolMemoryStream(initialBuffer, 0, initialBuffer.Length, true, true, false, true))
                        {
                            var maximumPacketSize = this.NegotiatedOptions.ServerOptions.MaximumPacketSize;

                            int packetCount = 0;
                            while (this._outgoingPackets.TryDequeue(out var packet))
                            {
                                // calculate packet size only if there's a valid value
                                if (maximumPacketSize > 0 && maximumPacketSize < UInt32.MaxValue)
                                {
                                    // The Client MUST NOT send packets exceeding Maximum Packet Size to the Server [MQTT-3.2.2-15]. (https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901086)
                                    UInt64 packetSize = packet.CalculatePacketSize();
                                    if (packetSize > maximumPacketSize)
                                    {
                                        Logger.Warning(nameof(MQTTClient), $"Skipping Packet({packet.Type}) because reached({packetSize}) server's maximum payload size limit({this.NegotiatedOptions.ServerOptions.MaximumPacketSize})!", this.Context);
                                        continue;
                                    }
                                    else
                                        packet.EncodeInto(ms);
                                }
                                else
                                    packet.EncodeInto(ms);

                                packetCount++;
                            }

                            var sendBuffer = ms.GetBuffer();

                            Logger.Information(nameof(MQTTClient), $"{nameof(EndPacketBuffer)}: Sending {packetCount:N0} packet(s) encoded in {ms.Position:N0} bytes...", this.Context);

                            this.transport.Send(new BufferSegment(sendBuffer, 0, (int)ms.Position));
                        }

                        this.lastPacketSentAt = DateTime.Now;
                    }
                }
                catch (MQTTException ex)
                {
                    if (initialBuffer != null)
                        BufferPool.Release(initialBuffer);

                    Logger.Exception(nameof(MQTTClient), nameof(EndPacketBuffer), ex, this.Context);
                    this.MQTTError(nameof(EndPacketBuffer), ex);
                }
                catch (Exception ex)
                {
                    if (initialBuffer != null)
                        BufferPool.Release(initialBuffer);

                    Logger.Exception(nameof(MQTTClient), nameof(EndPacketBuffer), ex, this.Context);
                }
            }
        }

        /// <summary>
        /// Creates and returns with a ConnectPacketBuilder instance.
        /// </summary>
        public ConnectPacketBuilder CreateConnectPacketBuilder() => new ConnectPacketBuilder(this);

        /// <summary>
        /// Starts connecting to the broker. It's a non-blocking method.
        /// </summary>
        public void BeginConnect(ConnectPacketBuilderDelegate connectPacketBuilderCallback, CancellationToken token = default)
        {
            if (connectPacketBuilderCallback == null)
                throw new ArgumentNullException(nameof(connectPacketBuilderCallback));

            Logger.Information(nameof(MQTTClient), nameof(BeginConnect), this.Context);
            if (this.State != ClientStates.Initial)
                throw new Exception("This client is already used.");

            if (this.connectBag == null)
                this.connectBag = new ConnectBag();
            this.connectBag.connectPacketBuilderFactory = connectPacketBuilderCallback;

            this.State = ClientStates.TransportConnecting;

            switch (this.Options.Transport)
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                case SupportedTransports.TCP:
                    this.transport = new SecureTCPTransport(this);
                    break;
#endif

                case SupportedTransports.WebSocket:
                    this.transport = new WebSocketTransport(this);
                    break;
            }

            this.transport.BeginConnect(token);
        }

        /// <summary>
        /// Starts connecting to the broker. 
        /// </summary>
        public Task<MQTTClient> ConnectAsync(ConnectPacketBuilderDelegate connectPacketBuilder, CancellationToken token = default)
        {
            if (this.connectBag == null)
                this.connectBag = new ConnectBag();
            this.connectBag.completionSource = new TaskCompletionSource<MQTTClient>(TaskCreationOptions.RunContinuationsAsynchronously);

            this.BeginConnect(connectPacketBuilder, token);

            return this.connectBag.completionSource.Task;
        }

        /// <summary>
        /// Creates and returns with a DisconnectPacketBuilder instance.
        /// </summary>
        public DisconnectPacketBuilder CreateDisconnectPacketBuilder() => new DisconnectPacketBuilder(this);
        internal void BeginDisconnect(in DisconnectPacketBuilder builder)
        {
            if (this.State < ClientStates.TransportConnecting || this.State > ClientStates.Connected)
                return;

            Logger.Information(nameof(MQTTClient), $"{nameof(BeginDisconnect)}({nameof(MQTTClient)}: {this.State}, Transport: {this.transport?.State})", this.Context);

            this.Session?.QueuedPackets.Clear(false);

            if (this.State > ClientStates.Connected)
                return;

            if (this.State == ClientStates.Connected)
            {
                var disconnectPacket = builder.Build();
                this.Send(in disconnectPacket);
            }

            if (this.State >= ClientStates.TransportConnecting)
            {
                this.State = ClientStates.Disconnecting;
                this.transport.BeginDisconnect();
            }
            else
            {
                //this.State = ClientStates.Disconnected;
                SetDisconnected(DisconnectReasonCodes.NormalDisconnection, string.Empty);
            }

            if (IsQuitting)
            {
                //this.State = ClientStates.Disconnected;
                SetDisconnected(DisconnectReasonCodes.NormalDisconnection, string.Empty);
            }
        }

        /// <summary>
        /// Creates and returns with a SubscribePacketBuilder. 
        /// </summary>
        public SubscribePacketBuilder CreateSubscriptionBuilder(string topicFilter) => new SubscribePacketBuilder(this, topicFilter ?? throw new ArgumentNullException(nameof(topicFilter)));

        /// <summary>
        /// Creates and returns with a BulkSubscribePacketBuilder instance.
        /// </summary>
        public BulkSubscribePacketBuilder CreateBulkSubscriptionBuilder() => new BulkSubscribePacketBuilder(this);

        internal Subscription BeginSubscribe(in BulkSubscribePacketBuilder builder)
        {
            if (this.State != ClientStates.Connected)
                throw new Exception($"Not connected! Current state: {this.State}");

            var (packet, subscription) = builder.Build(this);

            this.Send(in packet);

            return subscription;
        }

        /// <summary>
        /// Creates and returns with an UnsubscribePacketBuilder instance.
        /// </summary>
        public UnsubscribePacketBuilder CreateUnsubscribePacketBuilder(string topicFilter) => new UnsubscribePacketBuilder(this, topicFilter ?? throw new ArgumentNullException(nameof(topicFilter)));

        /// <summary>
        /// Creates and returns with a BulkUnsubscribePacketBuilder instance.
        /// </summary>
        public BulkUnsubscribePacketBuilder CreateBulkUnsubscribePacketBuilder() => new BulkUnsubscribePacketBuilder(this);

        internal void BeginUnsubscribe(in BulkUnsubscribePacketBuilder builder)
        {
            if (this.State != ClientStates.Connected)
                throw new Exception($"Not connected! Current state: {this.State}");

            var outPacket = builder.Build();
            this.Send(in outPacket);
        }

        /// <summary>
        /// Adds a new topic alias.
        /// </summary>
        public void AddTopicAlias(string topicName)
        {
            BestMQTT.Packets.Utils.ExceptionHelper.ThrowIfV311(this.Options.ProtocolVersion, $"{nameof(AddTopicAlias)} is available with MQTT v5.0 or newer.");

            if (string.IsNullOrEmpty(topicName))
                throw new ArgumentNullException(nameof(topicName));

            if (this.State != ClientStates.Connected)
                throw new Exception("Can add an alias only when connected!");

            UInt16 aliases = this.Session.ClientTopicAliasMapping.Count();
            if (aliases >= this.NegotiatedOptions.ServerOptions.TopicAliasMaximum)
                throw new Exception($"Can't add more alias, already reached the server's Topic Alias Maximum setting ({this.NegotiatedOptions.ServerOptions.TopicAliasMaximum})!");

            this.Session.ClientTopicAliasMapping.Add(topicName, this.NegotiatedOptions.ServerOptions.TopicAliasMaximum);
        }

        /// <summary>
        /// Creates and returns with an ApplicationMessagePacketBuilder instance.
        /// </summary>
        public ApplicationMessagePacketBuilder CreateApplicationMessageBuilder(string topicName) => new ApplicationMessagePacketBuilder(this).WithTopicName(topicName);
        internal void BeginPublish(in ApplicationMessagePacketBuilder builder)
        {
            if (this.State != ClientStates.Connected)
                throw new Exception($"Not connected! Current state: {this.State}");

            // If the Server included a Maximum QoS in its CONNACK response to a Client and it receives a PUBLISH packet with a QoS greater than this,
            // then it uses DISCONNECT with Reason Code 0x9B (QoS not supported) (https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901103)
            if (this.NegotiatedOptions.ServerOptions.MaximumQoS > builder.QoS)
                builder.WithQoS(this.NegotiatedOptions.ServerOptions.MaximumQoS);

            // Topic Alias
            if (builder.PropertyBuilder.Properties.TryFindData(PacketProperties.TopicAlias, DataTypes.TwoByteInteger, out var topicAlias))
            {
                if (string.IsNullOrEmpty(builder.TopicName))
                    throw new ArgumentException($"TopicName is empty while TopicAlias({topicAlias.Integer}) is set");

                this.Session.ClientTopicAliasMapping.Set((UInt16)topicAlias.Integer, builder.TopicName);
            }
            else
            {
                var (alias, sentToServer) = this.Session.ClientTopicAliasMapping.Find(builder.TopicName);
                if (alias > 0 && alias <= this.NegotiatedOptions.ServerOptions.TopicAliasMaximum)
                {
                    // The first publish message must send both the alias and topic name to the server.
                    if (!sentToServer)
                        this.Session.ClientTopicAliasMapping.SetSent(alias, sentToServer: true);
                    else
                        builder.WithTopicName(string.Empty);
                    builder.PropertyBuilder.WithTopicAlias(alias);
                }
                //else
                //{
                //    throw new Exception("Neither Topic Alias or Topic Name could be found in the builder!");
                //}
            }

            var packet = builder.Build(this);

            // Each time the Client or Server sends a PUBLISH packet at QoS > 0, it decrements the send quota.
            // If the send quota reaches zero, the Client or Server MUST NOT send any more PUBLISH packets with QoS > 0 [MQTT-4.9.0-2].
            bool queuePacket = builder.QoS > QoSLevels.AtMostOnceDelivery && this._sendQuota == 0;

            if (queuePacket)
            {
                Logger.Verbose(nameof(MQTTClient), $"{nameof(BeginPublish)} queuing packet ({builder.PacketID}) with QoS({builder.QoS}) and quota({this._sendQuota})", this.Context);
                this.Session.QueuedPackets.Add(builder.PacketID, in packet);
            }
            else
                SendPublishPacket(builder.PacketID, in packet);
        }

        /// <summary>
        /// Creates and returns with an AuthenticationPacketBuilder instance.
        /// </summary>
        public AuthenticationPacketBuilder CreateAuthenticationPacketBuilder() => new AuthenticationPacketBuilder(this);
        internal void BeginAuthentication(in AuthenticationPacketBuilder builder)
        {
            var outPacket = builder.Build();
            this.Send(in outPacket);
        }

        /// <summary>
        /// Private class to hold connection related information. These are needed only while connecting.
        /// </summary>
        class ConnectBag
        {
            //public ConnectPacketBuilder builder;
            public ConnectPacketBuilderDelegate connectPacketBuilderFactory;

            public string errorReason;
            public TaskCompletionSource<MQTTClient> completionSource;
        }
    }
}
