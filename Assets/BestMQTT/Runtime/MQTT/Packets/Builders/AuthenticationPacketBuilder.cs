using System;

using BestMQTT.Packets.Utils;

namespace BestMQTT.Packets.Builders
{
    /// <summary>
    /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901217
    /// </summary>
    public struct AuthenticationPacketBuilder
    {
        private MQTTClient _client;

        private AuthReasonCodes _reasonCode;
        private AuthenticationPropertiesBuilder _propertiesBuilder;        

        public AuthenticationPacketBuilder(MQTTClient client)
        {
            this._client = client;

            this._reasonCode = AuthReasonCodes.Success;
            this._propertiesBuilder = default(AuthenticationPropertiesBuilder);

            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(AuthenticationPacketBuilder)} is available with MQTT v5.0 or newer.");
        }

        public AuthenticationPacketBuilder WithReasonCode(AuthReasonCodes authReason)
        {
            this._reasonCode = authReason;
            return this;
        }

        public AuthenticationPacketBuilder WithAuthenticationMethod(string method)
        {
            this._propertiesBuilder.WithAuthenticationMethod(method);

            return this;
        }

        public AuthenticationPacketBuilder WithAuthenticationData(byte[] data)
        {
            this._propertiesBuilder.WithAuthenticationData(data);

            return this;
        }

        public AuthenticationPacketBuilder WithReasonString(string reason)
        {
            this._propertiesBuilder.WithReasonString(reason);

            return this;
        }

        public AuthenticationPacketBuilder WithUserProperty(string key, string value)
        {
            this._propertiesBuilder.WithUserProperty(key, value);

            return this;
        }

        internal AuthenticationPacketBuilder WithPropertyBuilder(AuthenticationPropertiesBuilder builder)
        {
            this._propertiesBuilder = builder;

            return this;
        }

        public void BeginAuthenticate() => this._client.BeginAuthentication(this);

        internal Packet Build()
        {
            Packet packet = new Packet();
            packet.Type = PacketTypes.Auth;

            // The Reason Code and Property Length can be omitted if the Reason Code is 0x00 (Success) and there are no Properties. In this case the AUTH has a Remaining Length of 0.
            if (this._reasonCode != AuthReasonCodes.Success || this._propertiesBuilder.Count != 0)
            {
                packet.AddVariableHeader(Data.FromByte((byte)this._reasonCode));
                packet.AddVariableHeader(Data.FromProperties(this._propertiesBuilder.Build()));
            }

            return packet;
        }
    }

    internal struct AuthenticationPropertiesBuilder
    {
        private Properties _properties;
        internal int Count { get => this._properties.Count; }

        public AuthenticationPropertiesBuilder WithAuthenticationMethod(string method)
        {
            this._properties.ThrowIfPresent(PacketProperties.AuthenticationMethod);
            this._properties.AddProperty(PacketProperties.AuthenticationMethod, Data.FromString(method));

            return this;
        }

        public AuthenticationPropertiesBuilder WithAuthenticationData(byte[] data)
        {
            this._properties.ThrowIfPresent(PacketProperties.AuthenticationData);
            this._properties.AddProperty(PacketProperties.AuthenticationData, Data.FromArray(data));

            return this;
        }

        public AuthenticationPropertiesBuilder WithReasonString(string reason)
        {
            this._properties.ThrowIfPresent(PacketProperties.ReasonString);
            this._properties.AddProperty(PacketProperties.ReasonString, Data.FromString(reason));

            return this;
        }

        public AuthenticationPropertiesBuilder WithUserProperty(string key, string value)
        {
            this._properties.AddProperty(PacketProperties.UserProperty, Data.FromStringPair(key, value));

            return this;
        }

        internal Properties Build() => this._properties;
    }
}
