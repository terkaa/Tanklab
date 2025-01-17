using System;

using BestMQTT.Packets.Utils;

namespace BestMQTT.Packets.Builders
{
    /// <summary>
    /// Builder to create an application message.
    /// </summary>
    /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901100"/>
    public struct ApplicationMessagePacketBuilder
    {
        private MQTTClient _client;

        internal QoSLevels QoS { get => this._qos; }
        internal string TopicName { get => this._topicName; }
        internal UInt16 PacketID { get => this._packetId; }
        internal PublishPropertyBuilder PropertyBuilder { get => this._propertyBuilder; }

        private BitField _flags;
        private QoSLevels _qos;
        private string _topicName;
        private UInt16 _packetId;

        private PublishPropertyBuilder _propertyBuilder;

        private byte[] _payload;
        

        internal ApplicationMessagePacketBuilder(MQTTClient client)
        {
            this._client = client;

            this._flags = new BitField(0);
            this._qos = QoSLevels.AtMostOnceDelivery;
            this._topicName = null;
            this._packetId = 0;

            this._propertyBuilder = default(PublishPropertyBuilder);
            this._payload = null;
        }

        /// <summary>
        /// Set the duplicate flag. (Not really used, it's set directly in MessageDeliveryRetry function)
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901102"/>
        internal ApplicationMessagePacketBuilder WithDuplicate()
        {
            this._flags[3] = true;
            return this;
        }

        /// <summary>
        /// Send the packet with a packet ID required for > QoS 0.
        /// </summary>
        internal ApplicationMessagePacketBuilder WithPacketId(UInt16 packetId)
        {
            if (packetId != 0 && this._qos == QoSLevels.AtMostOnceDelivery)
                throw new ArgumentException($"The Packet Identifier field is only present in PUBLISH packets where the QoS level is 1 or 2.");

            this._packetId = packetId;
            return this;
        }

        /// <summary>
        /// Build the packet with the given QoS level.
        /// </summary>
        public ApplicationMessagePacketBuilder WithQoS(QoSLevels qos)
        {
            this._qos = qos;

            switch (this._qos)
            {
                case QoSLevels.AtMostOnceDelivery: this._flags[2] = this._flags[1] = false; WithPacketId(0); break;
                case QoSLevels.AtLeastOnceDelivery: this._flags[2] = false; this._flags[1] = true; WithPacketId(this._client.GetNextPacketID()); break;
                case QoSLevels.ExactlyOnceDelivery: this._flags[2] = true; this._flags[1] = false; WithPacketId(this._client.GetNextPacketID()); break;
            }
            return this;
        }

        /// <summary>
        /// Build the packet with the given retain flag.
        /// </summary>
        public ApplicationMessagePacketBuilder WithRetain(bool retain = true)
        {
            this._flags[0] = retain;
            return this;
        }

        /// <summary>
        /// Build the packet with the given topic name.
        /// </summary>
        internal ApplicationMessagePacketBuilder WithTopicName(string topicName)
        {
            if (topicName.Contains("#") || topicName.Contains("+"))
                throw new ArgumentException("The Topic Name in the PUBLISH packet MUST NOT contain wildcard characters [MQTT-3.3.2-2].");
            this._topicName = topicName;
            return this;
        }

        /// <summary>
        /// Build the packet with the given payload format indicator.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901111"/>
        public ApplicationMessagePacketBuilder WithPayloadFormatIndicator(PayloadTypes payloadType)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithPayloadFormatIndicator)} is available with MQTT v5.0 or newer.");

            this._propertyBuilder.WithPayloadFormatIndicator(payloadType);
            return this;
        }

        /// <summary>
        /// Set the application message's expiry interval (it's in seconds).
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901112"/>
        public ApplicationMessagePacketBuilder WithMessageExpiryInterval(UInt32 seconds)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithMessageExpiryInterval)} is available with MQTT v5.0 or newer.");

            this._propertyBuilder.WithMessageExpiryInterval(seconds);
            return this;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901113
        /// </summary>
        internal ApplicationMessagePacketBuilder WithTopicAlias(UInt16 alias)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithTopicAlias)} is available with MQTT v5.0 or newer.");

            this._propertyBuilder.WithTopicAlias(alias);
            return this;
        }

        /// <summary>
        /// Set the application message's response topic.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901114"/>
        public ApplicationMessagePacketBuilder WithResponseTopic(string responseTopic)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithResponseTopic)} is available with MQTT v5.0 or newer.");

            this._propertyBuilder.WithResponseTopic(responseTopic);
            return this;
        }

        /// <summary>
        /// Optional data sent with the application message.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901115"/>
        public ApplicationMessagePacketBuilder WithCorrelationData(byte[] data)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithCorrelationData)} is available with MQTT v5.0 or newer.");

            this._propertyBuilder.WithCorrelationData(data);
            return this;
        }

        /// <summary>
        /// Optional key value pairs that will be sent with the application message.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901116"/>
        public ApplicationMessagePacketBuilder WithUserProperty(string key, string value)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithUserProperty)} is available with MQTT v5.0 or newer.");

            this._propertyBuilder.WithUserProperty(key, value);
            return this;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901117
        /// </summary>
        internal ApplicationMessagePacketBuilder WithSubscriptionIdentifier(UInt32 subscriptionId)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithSubscriptionIdentifier)} is available with MQTT v5.0 or newer.");

            this._propertyBuilder.WithSubscriptionIdentifier(subscriptionId);
            return this;
        }

        /// <summary>
        /// Optional Content-Type value to help process the application message's payload.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901118"/>
        public ApplicationMessagePacketBuilder WithContentType(string contentType)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithContentType)} is available with MQTT v5.0 or newer.");

            this._propertyBuilder.WithContentType(contentType);
            return this;
        }

        internal ApplicationMessagePacketBuilder WithProperties(PublishPropertyBuilder propertyBuilder)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithProperties)} is available with MQTT v5.0 or newer.");

            this._propertyBuilder = propertyBuilder;
            return this;
        }

        /// <summary>
        /// Set the application message's payload.
        /// </summary>
        public ApplicationMessagePacketBuilder WithPayload(byte[] payload)
        {
            this._payload = payload;
            return this;
        }

        /// <summary>
        /// Set the application message's payload. It also sets the payload format indicator to PayloadTypes.UTF8.
        /// </summary>
        public ApplicationMessagePacketBuilder WithPayload(string payload)
        {
            if (this._client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                this.WithPayloadFormatIndicator(PayloadTypes.UTF8);

            return this.WithPayload(System.Text.Encoding.UTF8.GetBytes(payload));
        }

        /// <summary>
        /// Begin sending the application message to the broker.
        /// </summary>
        public void BeginPublish() => this._client.BeginPublish(this);

        internal Packet Build(MQTTClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client), $"Use {nameof(ApplicationMessagePacketBuilder)} through {nameof(MQTTClient)}'s {nameof(MQTTClient.CreateApplicationMessageBuilder)}!");

            if (this._qos > client.NegotiatedOptions.ServerOptions.MaximumQoS)
                throw new ArgumentException($"QoS({this._qos}) is greater than server's maximum QoS({client.NegotiatedOptions.ServerOptions.MaximumQoS})");

            if (string.IsNullOrEmpty(this._topicName))
                throw new ArgumentException($"{nameof(TopicName)} must not be empty!");

            var packet = new Packet();
            packet.Type = PacketTypes.Publish;

            // reserved set
            this._flags[4] = true;

            packet.Flags = this._flags;

            // Topic Name
            packet.AddVariableHeader(Data.FromString(this._topicName));

            // Packet Identifier
            if (this._packetId > 0)
                packet.AddVariableHeader(Data.FromTwoByteInteger(this._packetId));

            // Properties
            var properties = this._propertyBuilder.Build();
            if (properties.TryFindData(PacketProperties.TopicAlias, DataTypes.TwoByteInteger, out var topicAlias))
            {
                // A Client MUST NOT send a PUBLISH packet with a Topic Alias greater than the Topic Alias Maximum value returned by the Server in the CONNACK packet [MQTT-3.3.2-9].
                if (topicAlias.Integer > client.NegotiatedOptions.ServerOptions.TopicAliasMaximum)
                    throw new ArgumentException($"A Client MUST NOT send a PUBLISH packet with a Topic Alias greater than the Topic Alias Maximum value({client.NegotiatedOptions.ServerOptions.TopicAliasMaximum}) returned by the Server in the CONNACK packet");
            }

            if (this._client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                packet.AddVariableHeader(Data.FromProperties(properties));

            // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901119
            // The length of the Payload can be calculated by subtracting the length of the Variable Header from the Remaining Length field that is in the Fixed Header.
            // It is valid for a PUBLISH packet to contain a zero length Payload.
            if (this._payload != null)
                packet.AddPayload(Data.FromRaw(this._payload));

            return packet;
        }
    }

    internal struct PublishPropertyBuilder
    {
        internal Properties Properties { get => this._properties; }
        private Properties _properties;

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901111
        /// </summary>
        public PublishPropertyBuilder WithPayloadFormatIndicator(PayloadTypes payloadType)
        {
            this._properties.AddProperty(PacketProperties.PayloadFormatIndicator, Data.FromByte((byte)payloadType));
            return this;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901112
        /// </summary>
        public PublishPropertyBuilder WithMessageExpiryInterval(UInt32 seconds)
        {
            this._properties.AddProperty(PacketProperties.MessageExpiryInterval, Data.FromFourByteInteger(seconds));
            return this;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901113
        /// </summary>
        public PublishPropertyBuilder WithTopicAlias(UInt16 alias)
        {
            if (alias == 0)
                throw new ArgumentException("A Topic Alias of 0 is not permitted.");

            this._properties.AddProperty(PacketProperties.TopicAlias, Data.FromTwoByteInteger(alias));
            return this;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901114
        /// </summary>
        public PublishPropertyBuilder WithResponseTopic(string responseTopic)
        {
            this._properties.AddProperty(PacketProperties.ResponseTopic, Data.FromString(responseTopic));
            return this;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901115
        /// </summary>
        public PublishPropertyBuilder WithCorrelationData(byte[] data)
        {
            this._properties.AddProperty(PacketProperties.CorrelationData, Data.FromArray(data));
            return this;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901116
        /// </summary>
        public PublishPropertyBuilder WithUserProperty(string key, string value)
        {
            this._properties.AddProperty(PacketProperties.UserProperty, Data.FromStringPair(key, value));
            return this;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901117
        /// </summary>
        internal PublishPropertyBuilder WithSubscriptionIdentifier(UInt32 subscriptionId)
        {
            this._properties.AddProperty(PacketProperties.SubscriptionIdentifier, Data.FromVariableByteInteger(subscriptionId));
            return this;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901118
        /// </summary>
        public PublishPropertyBuilder WithContentType(string contentType)
        {
            this._properties.AddProperty(PacketProperties.ContentType, Data.FromString(contentType));
            return this;
        }

        internal Properties Build() => this._properties;
    }
}
