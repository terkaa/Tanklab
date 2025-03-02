using System;

using BestMQTT.Packets.Utils;

namespace BestMQTT.Packets.Builders
{
    public enum DisconnectReasonCodes : byte
    {
        NormalDisconnection = 0x00,
        DisconnectWithWillMessage = 0x04,

        UnspecifiedError = 0x80,
        MalformedPacket = 0x81,
        ProtocolError = 0x82,
        ImplementationSpecificError = 0x83,
        NotAuthorized = 0x87,
        ServerBusy = 0x89,
        ServerShuttingDown = 0x8B,
        KeepAliveTimeout = 0x8D,
        SessionTakenOver = 0x8E,
        TopicFilterInvalid = 0x8F,
        TopicNameInvalid = 0x90,
        ReceiveMaximumExceeded = 0x93,
        TopicAliasInvalid = 0x94,
        PacketTooLarge = 0x95,
        MessageRateTooHigh = 0x96,
        QuotaExceeded = 0x97,
        AdministrativeAction = 0x98,
        PayloadFormatInvalid = 0x99,
        RetainNotSupported = 0x9A,
        QoSNotSupported = 0x9B,
        UseAnotherServer = 0x9C,
        ServerMoved = 0x9D,
        SharedSubscriptionsNotSupported = 0x9E,
        ConnectionRateExceeded = 0x9F,
        MaximumConnectTime = 0xA0,
        SubscriptionIdentifiersNotSupported = 0xA1,
        WildcardSubscriptionsNotSupported = 0xA2,
    }

    public struct DisconnectPacketBuilder
    {
        private MQTTClient _client;

        private DisconnectReasonCodes _reasonCode;
        private DisconnectPropertiesBuilder _propertiesBuilder;

        internal DisconnectPacketBuilder(MQTTClient client)
        {
            this._client = client;

            this._reasonCode = DisconnectReasonCodes.NormalDisconnection;
            this._propertiesBuilder = default(DisconnectPropertiesBuilder);
        }

        public DisconnectPacketBuilder WithReasonCode(DisconnectReasonCodes reasonCode)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithReasonCode)} is available with MQTT v5.0 or newer.");

            this._reasonCode = reasonCode;
            return this;
        }

        public DisconnectPacketBuilder WithSessionExpiryInterval(UInt32 seconds)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithSessionExpiryInterval)} is available with MQTT v5.0 or newer.");

            this._propertiesBuilder.WithSessionExpiryInterval(seconds);
            return this;
        }

        public DisconnectPacketBuilder WithReasonString(string reason)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithReasonString)} is available with MQTT v5.0 or newer.");

            this._propertiesBuilder.WithReasonString(reason);
            return this;
        }

        public DisconnectPacketBuilder WithUserProperty(string key, string value)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithUserProperty)} is available with MQTT v5.0 or newer.");

            this._propertiesBuilder.WithUserProperty(key, value);
            return this;
        }

        internal DisconnectPacketBuilder WithProperties(DisconnectPropertiesBuilder propertiesBuilder)
        {
            this._propertiesBuilder = propertiesBuilder;
            return this;
        }

        public void BeginDisconnect() => this._client.BeginDisconnect(this);

        internal Packet Build()
        {
            Packet packet = new Packet();

            packet.Type = PacketTypes.Disconnect;
            if (this._client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
            {
                packet.AddVariableHeader(Data.FromByte((byte)this._reasonCode));

                this._propertiesBuilder.Build(ref packet);
            }

            return packet;
        }
    }

    internal struct DisconnectPropertiesBuilder
    {
        private Properties _properties;

        public DisconnectPropertiesBuilder WithSessionExpiryInterval(UInt32 seconds)
        {
            this._properties.ThrowIfPresent(PacketProperties.SessionExpiryInterval);

            this._properties.AddProperty(new Property { Type = PacketProperties.SessionExpiryInterval, Data = Data.FromFourByteInteger(seconds) });
            return this;
        }

        public DisconnectPropertiesBuilder WithReasonString(string reason)
        {
            this._properties.ThrowIfPresent(PacketProperties.ReasonString);

            this._properties.AddProperty(new Property { Type = PacketProperties.ReasonString, Data = Data.FromString(reason) });
            return this;
        }

        public DisconnectPropertiesBuilder WithUserProperty(string key, string value)
        {
            this._properties.AddProperty(new Property { Type = PacketProperties.UserProperty, Data = Data.FromStringPair(key, value) });
            return this;
        }

        internal void Build(ref Packet packet)
        {
            packet.AddVariableHeader(Data.FromProperties(this._properties));
        }
    }
}
