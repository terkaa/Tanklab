using System;
using BestMQTT.Packets.Utils;

namespace BestMQTT.Packets.Builders
{
    public struct LastWillBuilder
    {
        private Data _topic;
        private Data _payload;
        private QoSLevels _qos;
        private bool _retain;
        private Properties _properties;

        internal void BuildFlags(ref BitField bitField)
        {
            if (this._retain)
                bitField[5] = true;

            // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901041
            switch (this._qos)
            {
                case QoSLevels.AtMostOnceDelivery: break;
                case QoSLevels.AtLeastOnceDelivery: bitField[3] = true; break;
                case QoSLevels.ExactlyOnceDelivery: bitField[4] = true; break;
            }
        }

        internal void Build(ref Packet packet)
        {

            // Will Properties
            packet.AddPayload(Data.FromProperties(this._properties));

            // Will Topic
            if (this._topic.IsSet)
                packet.AddPayload(this._topic);
            else
                throw new ArgumentException("Will Topic required!");

            // Will Payload
            if (this._payload.IsSet)
                packet.AddPayload(this._payload);
            else
                throw new ArgumentException("Will Payload required!");
        }

        /// <summary>
        /// Set the topic the last-will will be published.
        /// </summary>
        public LastWillBuilder WithTopic(string topic)
        {
            this._topic = Data.FromString(topic);
            return this;
        }

        /// <summary>
        /// Binary payload of the last-will.
        /// </summary>
        public LastWillBuilder WithPayload(byte[] binary)
        {
            this._payload = Data.FromArray(binary);
            return this;
        }

        /// <summary>
        /// Textual payload of the last-will. It also sets the Payload Format Indicator to UTF8.
        /// </summary>
        public LastWillBuilder WithPayload(string payload)
        {
            return this.WithPayload(System.Text.Encoding.UTF8.GetBytes(payload))
                       /*.WithPayloadFormatIndicator(PayloadTypes.UTF8)*/;
        }

        /// <summary>
        /// QoS level of the last-will.
        /// </summary>
        public LastWillBuilder WithQoS(QoSLevels qos)
        {
            this._qos = qos;
            return this;
        }

        /// <summary>
        /// Retain flag.
        /// </summary>
        public LastWillBuilder WithRetain(bool retain = true)
        {
            this._retain = retain;
            return this;
        }

        /// <summary>
        /// Delay before the broker will publish the last-will
        /// </summary>
        public LastWillBuilder WithDelayInterval(UInt32 seconds)
        {
            this._properties.ThrowIfPresent(PacketProperties.WillDelayInterval);

            this._properties.AddProperty(new Property() { Type = PacketProperties.WillDelayInterval, Data = Data.FromFourByteInteger(seconds) });
            return this;
        }

        /// <summary>
        /// Type of the payload, binary or textual.
        /// </summary>
        public LastWillBuilder WithPayloadFormatIndicator(PayloadTypes payloadType)
        {
            this._properties.ThrowIfPresent(PacketProperties.PayloadFormatIndicator);

            this._properties.AddProperty(new Property { Type = PacketProperties.PayloadFormatIndicator, Data = Data.FromByte((byte)payloadType) });
            return this;
        }

        public LastWillBuilder WithMessageExpiryInterval(UInt32 seconds)
        {
            this._properties.ThrowIfPresent(PacketProperties.MessageExpiryInterval);

            this._properties.AddProperty(new Property { Type = PacketProperties.MessageExpiryInterval, Data = Data.FromFourByteInteger(seconds) });
            return this;
        }

        public LastWillBuilder WithContentType(string contentType)
        {
            this._properties.ThrowIfPresent(PacketProperties.ContentType);

            this._properties.AddProperty(new Property { Type = PacketProperties.ContentType, Data = Data.FromString(contentType) });

            return this;
        }

        public LastWillBuilder WithResponseTopic(string topic)
        {
            this._properties.ThrowIfPresent(PacketProperties.ResponseTopic);

            this._properties.AddProperty(new Property { Type = PacketProperties.ResponseTopic, Data = Data.FromString(topic) });

            return this;
        }

        public LastWillBuilder WithCorrelationData(byte[] binary)
        {
            this._properties.ThrowIfPresent(PacketProperties.ResponseTopic);

            this._properties.AddProperty(new Property { Type = PacketProperties.CorrelationData, Data = Data.FromArray(binary) });

            return this;
        }

        public LastWillBuilder WithUserData(string key, string value)
        {
            this._properties.AddProperty(new Property { Type = PacketProperties.UserProperty, Data = Data.FromStringPair(key, value) });
            return this;
        }
    }

    public struct ConnectPacketBuilder
    {
        private MQTTClient _client;
        
        // Variable Headers
        private BitField _connectFlags;
        private UInt16 _keepAlive;
        private LastWillBuilder _lastWillBuilder;

        // Properties
        private ConnectPropertyBuilder _connectPropertyBuilder;

        // Payload
        private string _clientId;
        private Session _session;
        private string _userName;
        private string _password;

        internal ConnectPacketBuilder(MQTTClient client)
        {
            this._client = client;

            this._connectFlags = new BitField(0);
            this._keepAlive = 60;
            this._lastWillBuilder = default(LastWillBuilder);

            this._connectPropertyBuilder = default(ConnectPropertyBuilder);

            this._clientId = null;
            this._session = null;
            this._userName = null;
            this._password = null;
        }

        public ConnectPacketBuilder WithCleanStart()
        {
            this._connectFlags.Set(1, true);
            return this;
        }

        /// <summary>
        /// <see cref="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901045"/>
        /// </summary>
        public ConnectPacketBuilder WithKeepAlive(ushort seconds)
        {
            this._keepAlive = seconds;
            return this;
        }

        public ConnectPacketBuilder WithLastWill(LastWillBuilder lastWillBuilder)
        {
            this._lastWillBuilder = lastWillBuilder;
            this._connectFlags[2] = true;
            return this;
        }

        public ConnectPacketBuilder WithClientID(string clientId)
        {
            this._clientId = clientId;
            return this;
        }

        public ConnectPacketBuilder WithSession(Session session)
        {
            this._session = session;
            return this;
        }

        public ConnectPacketBuilder WithUserName(string userName)
        {
            this._userName = userName;
            this._connectFlags.Set(7, true);
            return this;
        }

        public ConnectPacketBuilder WithPassword(string password)
        {
            this._password = password;
            this._connectFlags.Set(6, true);
            return this;
        }

        public ConnectPacketBuilder WithUserNameAndPassword(string userName, string password)
        {
            return this.WithUserName(userName).WithPassword(password);
        }

        public ConnectPacketBuilder WithSessionExpiryInterval(UInt32 seconds)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithSessionExpiryInterval)} is available with MQTT v5.0 or newer.");

            this._connectPropertyBuilder.WithSessionExpiryInterval(seconds);
            return this;
        }

        public ConnectPacketBuilder WithReceiveMaximum(UInt16 value)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithReceiveMaximum)} is available with MQTT v5.0 or newer.");

            this._connectPropertyBuilder.WithReceiveMaximum(value);
            return this;
        }

        public ConnectPacketBuilder WithMaximumPacketSize(UInt32 maximumPacketSize)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithMaximumPacketSize)} is available with MQTT v5.0 or newer.");

            this._connectPropertyBuilder.WithMaximumPacketSize(maximumPacketSize);
            return this;
        }

        public ConnectPacketBuilder WithTopicAliasMaximum(UInt16 maximum)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithTopicAliasMaximum)} is available with MQTT v5.0 or newer.");

            this._connectPropertyBuilder.WithTopicAliasMaximum(maximum);
            return this;
        }

        public ConnectPacketBuilder WithRequestResponseInformation(bool request)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithRequestResponseInformation)} is available with MQTT v5.0 or newer.");

            this._connectPropertyBuilder.WithRequestResponseInformation(request);
            return this;
        }

        public ConnectPacketBuilder WithRequestProblemInformation(bool request)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithRequestProblemInformation)} is available with MQTT v5.0 or newer.");

            this._connectPropertyBuilder.WithRequestProblemInformation(request);
            return this;
        }

        public ConnectPacketBuilder WithUserProperty(string key, string value)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithUserProperty)} is available with MQTT v5.0 or newer.");

            this._connectPropertyBuilder.WithUserProperty(key, value);
            return this;
        }

        public ConnectPacketBuilder WithExtendedAuthenticationMethod(string method)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithExtendedAuthenticationMethod)} is available with MQTT v5.0 or newer.");

            this._connectPropertyBuilder.WithExtendedAuthenticationMethod(method);
            return this;
        }

        public ConnectPacketBuilder WithExtendedAuthenticationData(byte[] data)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithExtendedAuthenticationData)} is available with MQTT v5.0 or newer.");

            this._connectPropertyBuilder.WithExtendedAuthenticationData(data);
            return this;
        }

        internal ConnectPacketBuilder WithProperties(ConnectPropertyBuilder builder)
        {
            this._connectPropertyBuilder = builder;
            return this;
        }

        internal (Packet packet, Session session, ushort clientKeepAlive, uint clientMaximumPacketSize, ushort clientReceiveMaximum) Build()
        {
            // will flag
            if (this._connectFlags[2])
                this._lastWillBuilder.BuildFlags(ref this._connectFlags);

            var packet = new Packet();

            packet.Type = PacketTypes.Connect;

            packet.AddVariableHeader(Data.FromString("MQTT")); // Protocol Name

            switch(this._client.Options.ProtocolVersion)
            {
                case SupportedProtocolVersions.MQTT_3_1_1: break;
                case SupportedProtocolVersions.MQTT_5_0: break;
                default:
                    throw new NotImplementedException($"Version '{this._client.Options.ProtocolVersion}' isn't supported!");
            }

            byte protocolLevel = 0;
            switch(this._client.Options.ProtocolVersion)
            {
                // Protocol Level (v3.1.1): http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc398718030
                case SupportedProtocolVersions.MQTT_3_1_1: protocolLevel = 0x04; break;

                // Protocol Version (v5): https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901037
                case SupportedProtocolVersions.MQTT_5_0: protocolLevel = 0x05; break;

                default: throw new NotImplementedException($"Version '{this._client.Options.ProtocolVersion}' isn't supported!");
            };

            packet.AddVariableHeader(Data.FromByte(protocolLevel));

            packet.AddVariableHeader(this._connectFlags.AsData());
            packet.AddVariableHeader(Data.FromTwoByteInteger(this._keepAlive));

            var properties = this._connectPropertyBuilder.Build();
            if (this._client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                packet.AddVariableHeader(Data.FromProperties(properties));

            // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901058
            // These fields, if present, MUST appear in the order Client Identifier, Will Properties, Will Topic, Will Payload, User Name, Password

            // Client Identifier
            if (this._clientId != null)
                this._session = SessionHelper.Get(this._client.Options.Host, this._clientId);
            else if (this._session == null)
                this._session = SessionHelper.Get(this._client.Options.Host);

            if (this._session.IsNull)
            {
                // A Server MAY allow a Client to supply a ClientID that has a length of zero bytes, however if it does so the Server MUST treat this as a special case and assign a unique ClientID to that Client [MQTT-3.1.3-6].
                // It MUST then process the CONNECT packet as if the Client had provided that unique ClientID, and MUST return the Assigned Client Identifier in the CONNACK packet [MQTT-3.1.3-7].
                packet.SetPayload(Data.FromString(string.Empty));
            }
            else
                packet.SetPayload(Data.FromString(this._session.ClientId));

            // will flag
            if (this._connectFlags[2])
                this._lastWillBuilder.Build(ref packet);

            // User Name
            if (!string.IsNullOrEmpty(this._userName))
                packet.AddPayload(Data.FromString(this._userName));

            // Password
            if (!string.IsNullOrEmpty(this._password))
                packet.AddPayload(Data.FromString(this._password));

            // create & return with a tuple
            return (packet,
                this._session,
                this._keepAlive,
                properties.TryFindData(PacketProperties.MaximumPacketSize, DataTypes.FourByteInteger, out var maximumPacketSizeData) ? maximumPacketSizeData.Integer : UInt32.MaxValue,
                properties.TryFindData(PacketProperties.ReceiveMaximum, DataTypes.TwoByteInteger, out var receiveMaximumData) ? (UInt16)receiveMaximumData.Integer : UInt16.MaxValue);
        }
    }

    // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901046
    internal struct ConnectPropertyBuilder
    {
        private Properties _properties;

        public ConnectPropertyBuilder WithSessionExpiryInterval(UInt32 seconds)
        {
            this._properties.ThrowIfPresent(PacketProperties.SessionExpiryInterval);

            this._properties.AddProperty(new Property { Type = PacketProperties.SessionExpiryInterval, Data = Data.FromFourByteInteger(seconds) });
            return this;
        }

        public ConnectPropertyBuilder WithReceiveMaximum(UInt16 value)
        {
            if (value == 0)
                throw new ArgumentException($"{nameof(value)} must be larger than 0!");

            this._properties.ThrowIfPresent(PacketProperties.ReceiveMaximum);

            this._properties.AddProperty(new Property { Type = PacketProperties.ReceiveMaximum, Data = Data.FromTwoByteInteger(value) });
            return this;
        }

        public ConnectPropertyBuilder WithMaximumPacketSize(UInt32 maximumPacketSize)
        {
            if (maximumPacketSize == 0)
                throw new ArgumentException($"{nameof(maximumPacketSize)} must be larger than zero!");

            this._properties.ThrowIfPresent(PacketProperties.MaximumPacketSize);

            this._properties.AddProperty(new Property { Type = PacketProperties.MaximumPacketSize, Data = Data.FromFourByteInteger(maximumPacketSize) });
            return this;
        }

        public ConnectPropertyBuilder WithTopicAliasMaximum(UInt16 maximum)
        {
            this._properties.ThrowIfPresent(PacketProperties.TopicAliasMaximum);

            this._properties.AddProperty(new Property { Type = PacketProperties.TopicAliasMaximum, Data = Data.FromTwoByteInteger(maximum) });
            return this;
        }

        public ConnectPropertyBuilder WithRequestResponseInformation(bool request)
        {
            this._properties.ThrowIfPresent(PacketProperties.RequestResponseInformation);

            this._properties.AddProperty(new Property { Type = PacketProperties.RequestResponseInformation, Data = Data.FromByte(request ? 1 : 0) });

            return this;
        }

        public ConnectPropertyBuilder WithRequestProblemInformation(bool request)
        {
            this._properties.ThrowIfPresent(PacketProperties.RequestProblemInformation);

            this._properties.AddProperty(new Property { Type = PacketProperties.RequestProblemInformation, Data = Data.FromByte(request ? 1 : 0) });

            return this;
        }

        public ConnectPropertyBuilder WithUserProperty(string key, string value)
        {
            this._properties.AddProperty(new Property { Type = PacketProperties.UserProperty, Data = Data.FromStringPair(key, value) });
            return this;
        }

        public ConnectPropertyBuilder WithExtendedAuthenticationMethod(string method)
        {
            this._properties.ThrowIfPresent(PacketProperties.AuthenticationMethod);

            this._properties.AddProperty(new Property { Type = PacketProperties.AuthenticationMethod, Data = Data.FromString(method) });
            return this;
        }

        public ConnectPropertyBuilder WithExtendedAuthenticationData(byte[] data)
        {
            this._properties.ThrowIfPresent(PacketProperties.AuthenticationData);

            this._properties.AddProperty(new Property { Type = PacketProperties.AuthenticationData, Data = Data.FromArray(data) });
            return this;
        }

        internal Properties Build()
        {
            if (this._properties.Find(PacketProperties.TopicAliasMaximum).Data.Type == DataTypes.UnSet)
                WithTopicAliasMaximum(ushort.MaxValue);

            return this._properties;
        }
    }
}
