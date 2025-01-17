using System;
using System.Collections.Generic;
using System.IO;

using BestMQTT.Packets.Utils;

namespace BestMQTT.Packets.Readers
{
    internal static class PacketReaderImplementations
    {
        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901074
        /// </summary>
        public static Packet ReadConnectAckPacket(Stream stream, BitField flags, MQTTClient client)
        {
            PacketReaderHelpers.Expect(flags.Range(3, 0) == 0, () => $"{PacketTypes.ConnectAck} packet must have all zero flags! Received: {flags.Range(3, 0)}");

            var packet = new Packet();
            packet.Type = PacketTypes.ConnectAck;
            packet.Flags = flags;

            UInt32 remainingLength = DataEncoderHelper.DecodeVariableByteInteger(stream);

            PacketReaderHelpers.Expect(remainingLength > 0, () => $"{packet.Type} must have non-zero remaining length!");

            // Connect Acknowledge Flags
            packet.AddVariableHeader(Data.ReadAs(DataTypes.Bits, stream, ref remainingLength));

            // Connect Reason Code
            packet.AddVariableHeader(Data.ReadAs(DataTypes.Byte, stream, ref remainingLength));

            // Properties
            if (client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
            {
                if (remainingLength == 0)
                    throw new MQTTException(MQTTErrorTypes.MalformedPacket, $"Protocol version set to {client.Options.ProtocolVersion} but most probably the server trying to communicate with {SupportedProtocolVersions.MQTT_3_1_1}! Connect Ack - no more bytes left to read variable header!");
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Property, stream, ref remainingLength));
            }

            return packet;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901205
        /// </summary>
        public static Packet ReadDisconnectPacket(Stream stream, BitField flags, MQTTClient client)
        {
            PacketReaderHelpers.Expect(flags.Range(3, 0) == 0, () => $"{PacketTypes.Disconnect} packet must have all zero flags! Received: {flags.Range(3, 0)}");

            var packet = new Packet();
            packet.Type = PacketTypes.Disconnect;
            packet.Flags = flags;

            UInt32 remainingLength = DataEncoderHelper.DecodeVariableByteInteger(stream);

            // Byte 1 in the Variable Header is the Disconnect Reason Code. If the Remaining Length is less than 1 the value of 0x00 (Normal disconnection) is used.
            if (remainingLength < 1)
            {
                packet.AddVariableHeader(Data.FromByte(0));
            }
            else
            {
                // Disconnect Reason Code
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Byte, stream, ref remainingLength));

                // Properties
                // The length of Properties in the DISCONNECT packet Variable Header encoded as a Variable Byte Integer. If the Remaining Length is less than 2, a value of 0 is used.
                if (remainingLength >= 2 && client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                    packet.AddVariableHeader(Data.ReadAs(DataTypes.Property, stream, ref remainingLength));
            }

            return packet;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901171
        /// </summary>
        public static Packet ReadSubscribeAckPacket(Stream stream, BitField flags, MQTTClient client)
        {
            PacketReaderHelpers.Expect(flags.Range(3, 0) == 0, () => $"{PacketTypes.SubscribeAck} packet must have all zero flags! Received: {flags.Range(3, 0)}");

            var packet = new Packet();
            packet.Type = PacketTypes.SubscribeAck;
            packet.Flags = flags;

            UInt32 remainingLength = DataEncoderHelper.DecodeVariableByteInteger(stream);

            PacketReaderHelpers.Expect(remainingLength > 0, () => $"{packet.Type} must have non-zero remaining length!");

            packet.AddVariableHeader(Data.ReadAs(DataTypes.TwoByteInteger, stream, ref remainingLength));

            if (client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Property, stream, ref remainingLength));

            while (remainingLength > 0)
                packet.Payload.Add(Data.ReadAs(DataTypes.Byte, stream, ref remainingLength));

            return packet;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901187
        /// </summary>
        public static Packet ReadUnsubscribeAckPacket(Stream stream, BitField flags, MQTTClient client)
        {
            PacketReaderHelpers.Expect(flags.Range(3, 0) == 0, () => $"{PacketTypes.SubscribeAck} packet must have all zero flags! Received: {flags.Range(3, 0)}");

            var packet = new Packet();
            packet.Type = PacketTypes.UnsubscribeAck;

            UInt32 remainingLength = DataEncoderHelper.DecodeVariableByteInteger(stream);

            PacketReaderHelpers.Expect(remainingLength > 0, () => $"{packet.Type} must have non-zero remaining length!");

            packet.AddVariableHeader(Data.ReadAs(DataTypes.TwoByteInteger, stream, ref remainingLength));

            if (client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Property, stream, ref remainingLength));

            while (remainingLength > 0)
                packet.Payload.Add(Data.ReadAs(DataTypes.Byte, stream, ref remainingLength));

            return packet;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901100
        /// </summary>
        public static Packet ReadPublishPacket(Stream stream, BitField flags, MQTTClient client)
        {
            var packet = new Packet();
            packet.Type = PacketTypes.Publish;
            packet.Flags = flags;

            UInt32 remainingLength = DataEncoderHelper.DecodeVariableByteInteger(stream);

            packet.AddVariableHeader(Data.ReadAs(DataTypes.UTF8String, stream, ref remainingLength));

            // The Packet Identifier field is only present in PUBLISH packets where the QoS level is 1 or 2.
            if (flags.Range(2, 1) > 0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.TwoByteInteger, stream, ref remainingLength));

            if (client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Property, stream, ref remainingLength));

            if (remainingLength > 0)
                packet.Payload.Add(Data.ReadAs(DataTypes.Raw, stream, ref remainingLength));

            return packet;
        }

        private static List<ApplicationMessage> applicationMessages = new List<ApplicationMessage>();

        public static List<ApplicationMessage> CreateApplicationMessagesV311(Packet packet, System.Collections.Concurrent.ConcurrentDictionary<uint, Subscription> subscriptions)
        {
            applicationMessages.Clear();

            var topicName = packet.VariableHeaderFields[0].UTF8String.Key;
            foreach (var kvp in subscriptions)
                if (kvp.Value.HasMatchingTopic(topicName))
                    applicationMessages.Add(new ApplicationMessage(kvp.Key, packet));

            return applicationMessages;

        }

        public static List<ApplicationMessage> CreateApplicationMessages(Packet packet)
        {
            applicationMessages.Clear();

            packet.VariableHeaderFields.Properties.ForEach(PacketProperties.SubscriptionIdentifier, data =>
            {
                var result = new ApplicationMessage(data.Integer, packet);
                applicationMessages.Add(result);
            });

            return applicationMessages;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901131
        /// </summary>
        public static Packet ReadPublishReleasePacket(Stream stream, BitField flags, MQTTClient client)
        {
            PacketReaderHelpers.Expect(flags.Range(3, 0) == 0b0010, () => $"{PacketTypes.PublishRelease} packet must have {0b0010} flags! Received: {flags.Range(3, 0)}");

            var packet = new Packet();
            packet.Type = PacketTypes.PublishRelease;
            packet.Flags = flags;

            UInt32 remainingLength = DataEncoderHelper.DecodeVariableByteInteger(stream);

            PacketReaderHelpers.Expect(remainingLength > 0, () => $"{packet.Type} must have non-zero remaining length!");

            // packetID
            packet.AddVariableHeader(Data.ReadAs(DataTypes.TwoByteInteger, stream, ref remainingLength));

            // The Reason Code and Property Length can be omitted if the Reason Code is 0x00 (Success) and there are no Properties.
            if (remainingLength > 0)
            {
                // Reason Code
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Byte, stream, ref remainingLength));

                // Properties
                if (client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                    packet.AddVariableHeader(Data.ReadAs(DataTypes.Property, stream, ref remainingLength));
            }

            return packet;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901131
        /// </summary>
        public static Packet ReadPublishReceivedPacket(Stream stream, BitField flags, MQTTClient client)
        {
            var packet = new Packet();
            packet.Type = PacketTypes.PublishReceived;
            packet.Flags = flags;

            UInt32 remainingLength = DataEncoderHelper.DecodeVariableByteInteger(stream);

            // Packet Identifier
            packet.AddVariableHeader(Data.ReadAs(DataTypes.TwoByteInteger, stream, ref remainingLength));

            // Reason Code
            if (remainingLength > 0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Byte, stream, ref remainingLength));
            else
                packet.AddVariableHeader(Data.FromByte((byte)PublishAckAndReceivedReasonCodes.Success));

            // Property
            if (remainingLength > 0 && client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Property, stream, ref remainingLength));

            return packet;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901151
        /// </summary>
        public static Packet ReadPublishCompletePacket(Stream stream, BitField flags, MQTTClient client)
        {
            var packet = new Packet();
            packet.Type = PacketTypes.PublishComplete;
            packet.Flags = flags;

            UInt32 remainingLength = DataEncoderHelper.DecodeVariableByteInteger(stream);

            // Packet Identifier
            packet.AddVariableHeader(Data.ReadAs(DataTypes.TwoByteInteger, stream, ref remainingLength));

            // Reason Code
            if (remainingLength > 0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Byte, stream, ref remainingLength));
            else
                packet.AddVariableHeader(Data.FromByte((byte)PublishReleaseAndCompleteReasonCodes.Success));

            // Property
            if (remainingLength > 0 && client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Property, stream, ref remainingLength));

            return packet;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901121
        /// </summary>
        public static Packet ReadPublishAckPacket(Stream stream, BitField flags, MQTTClient client)
        {
            var packet = new Packet();
            packet.Type = PacketTypes.PublishAck;
            packet.Flags = flags;

            UInt32 remainingLength = DataEncoderHelper.DecodeVariableByteInteger(stream);

            // Packet Identifier
            packet.AddVariableHeader(Data.ReadAs(DataTypes.TwoByteInteger, stream, ref remainingLength));

            // Reason Code
            if (remainingLength > 0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Byte, stream, ref remainingLength));
            else
                packet.AddVariableHeader(Data.FromByte((byte)PublishAckAndReceivedReasonCodes.Success));

            // Property
            if (remainingLength > 0 && client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Property, stream, ref remainingLength));

            return packet;
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901217
        /// </summary>
        public static Packet ReadAuthPacket(Stream stream, BitField flags, MQTTClient client)
        {
            if (client.Options.ProtocolVersion < SupportedProtocolVersions.MQTT_5_0)
                throw new MQTTException(MQTTErrorTypes.ProtocolError, "Auth packet encountered while SupportedVersion is set to v3.1.1!");

            var packet = new Packet();
            packet.Type = PacketTypes.Auth;
            packet.Flags = flags;

            UInt32 remainingLength = DataEncoderHelper.DecodeVariableByteInteger(stream);

            // The Reason Code and Property Length can be omitted if the Reason Code is 0x00 (Success) and there are no Properties.
            // In this case the AUTH has a Remaining Length of 0.

            // Reason Code
            if (remainingLength > 0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Byte, stream, ref remainingLength));
            else
                packet.AddVariableHeader(Data.FromByte((byte)AuthReasonCodes.Success));

            // Property
            if (remainingLength > 0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.Property, stream, ref remainingLength));
            else
                packet.AddVariableHeader(Data.FromProperties(new Properties()));

            return packet;
        }
    }
}
