using System;

using BestMQTT.Packets.Utils;
using BestMQTT.Transports;

namespace BestMQTT.Packets.Readers
{
    internal static class IncomingPacketReader
    {
        internal static (bool success, Packet packet) TryToReadFrom(Transport transport)
        {
            var stream = transport.ReceiveStream;
            UInt32 clientMaximumPacketSize = transport.Parent.NegotiatedOptions.ClientMaximumPacketSize;

            if (stream.Length < 2)
            {
                BestHTTP.HTTPManager.Logger.Information(nameof(IncomingPacketReader), $"{nameof(TryToReadFrom)}: Not enough data for fixed header! Available: {stream.Length:N0}", transport.Context);
                return (false, Packet.Empty());
            }

            stream.BeginPeek();
            BitField bitField = new BitField((byte)stream.PeekByte());

            try
            {
                var (success, value) = DataEncoderHelper.TryToDecodeVariableByteInteger(stream);
                if (!success)
                {
                    BestHTTP.HTTPManager.Logger.Information(nameof(IncomingPacketReader), $"{nameof(TryToReadFrom)}: Not enough data for remaining length! Available: {stream.Length:N0}", transport.Context);
                    return (false, Packet.Empty());
                }

                UInt32 packetSize = (UInt32)(1 + DataEncoderHelper.CalculateRequiredBytesForVariableByteInteger(value) + value);

                if (packetSize > clientMaximumPacketSize)
                    throw new MQTTException(MQTTErrorTypes.PacketTooLarge, $"Packet({(PacketTypes)bitField.Range(7, 4)}) has larger size({packetSize:N0}) than client's settings({clientMaximumPacketSize:N0})!");

                if (stream.Length < packetSize)
                {
                    BestHTTP.HTTPManager.Logger.Information(nameof(IncomingPacketReader), $"{nameof(TryToReadFrom)}: Not all data available for the whole packet! Available: {stream.Length:N0}, packet size: {packetSize:N0}", transport.Context);
                    return (false, Packet.Empty());
                }

                BitField firstByte = new BitField((byte)stream.ReadByte());
                PacketTypes type = (PacketTypes)firstByte.Range(7, 4);

                firstByte.ClearRange(7, 4);

                Packet packet;
                switch (type)
                {
                    case PacketTypes.ConnectAck: packet = PacketReaderImplementations.ReadConnectAckPacket(stream, firstByte, transport.Parent); break;
                    case PacketTypes.Disconnect: packet = PacketReaderImplementations.ReadDisconnectPacket(stream, firstByte, transport.Parent); break;

                    case PacketTypes.SubscribeAck: packet = PacketReaderImplementations.ReadSubscribeAckPacket(stream, firstByte, transport.Parent); break;
                    case PacketTypes.UnsubscribeAck: packet = PacketReaderImplementations.ReadUnsubscribeAckPacket(stream, firstByte, transport.Parent); break;

                    case PacketTypes.Publish: packet = PacketReaderImplementations.ReadPublishPacket(stream, firstByte, transport.Parent); break;

                    case PacketTypes.PublishRelease: packet = PacketReaderImplementations.ReadPublishReleasePacket(stream, firstByte, transport.Parent); break;
                    case PacketTypes.PublishReceived: packet = PacketReaderImplementations.ReadPublishReceivedPacket(stream, firstByte, transport.Parent); break;
                    case PacketTypes.PublishComplete: packet = PacketReaderImplementations.ReadPublishCompletePacket(stream, firstByte, transport.Parent); break;

                    case PacketTypes.PublishAck: packet = PacketReaderImplementations.ReadPublishAckPacket(stream, firstByte, transport.Parent); break;

                    // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901200
                    case PacketTypes.PingResponse:
                        var remainingLength = DataEncoderHelper.DecodeVariableByteInteger(stream);
                        if (remainingLength != 0)
                            throw new MalformedPacketException("Ping Response expected to have 0 Remaining Length!");
                        packet = new Packet { Type = PacketTypes.PingResponse };
                        break;

                    case PacketTypes.Auth: packet = PacketReaderImplementations.ReadAuthPacket(stream, firstByte, transport.Parent); break;

                    default:
                        throw new NotImplementedException($"Can't read type '{type}'");
                }

                return (true, packet);
            }
            catch (MQTTException)
            {
                throw;
            }
            catch(Exception ex)
            {
                BestHTTP.HTTPManager.Logger.Exception(nameof(IncomingPacketReader), $"{nameof(TryToReadFrom)}", ex, transport.Context);
                return (false, Packet.Empty());
            }
        }
    }
}
