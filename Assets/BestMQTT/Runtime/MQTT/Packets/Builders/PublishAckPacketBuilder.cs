using System;

using BestMQTT.Packets.Utils;

namespace BestMQTT.Packets.Builders
{
    /// <summary>
    /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901121
    /// </summary>
    internal struct PublishAckBuilder
    {
        MQTTClient _client;
        UInt16 _packetId;
        PublishAckAndReceivedReasonCodes _reasonCode;

        Properties _properties;

        public PublishAckBuilder(MQTTClient client)
        {
            this._client = client;
            this._packetId = 0;
            this._reasonCode = PublishAckAndReceivedReasonCodes.Success;
            this._properties = default(Properties);
        }

        public PublishAckBuilder WithPacketID(UInt16 pid)
        {
            this._packetId = pid;

            return this;
        }

        public PublishAckBuilder WithReasonCode(PublishAckAndReceivedReasonCodes code)
        {
            this._reasonCode = code;

            return this;
        }

        public PublishAckBuilder WithReasonString(string reason)
        {
            this._properties.AddProperty(new Property { Type = PacketProperties.ReasonString, Data = Data.FromString(reason) });

            return this;
        }

        public PublishAckBuilder WithUserProperty(string key, string value)
        {
            this._properties.AddProperty(new Property { Type = PacketProperties.UserProperty, Data = Data.FromStringPair(key, value) });

            return this;
        }

        public Packet Build()
        {
            var packet = new Packet();
            packet.Type = PacketTypes.PublishAck;
            packet.Flags = new BitField(0);

            packet.AddVariableHeader(Data.FromTwoByteInteger(this._packetId));

            if (this._client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
            {
                packet.AddVariableHeader(Data.FromByte((byte)this._reasonCode));
                packet.AddVariableHeader(Data.FromProperties(this._properties));
            }

            return packet;
        }
    }
}
