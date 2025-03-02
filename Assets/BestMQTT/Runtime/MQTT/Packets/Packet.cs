using System;
using System.IO;

using BestMQTT.Packets.Utils;

namespace BestMQTT.Packets
{
    public struct Packet
    {
        public PacketTypes Type;
        public BitField Flags;

        public DataContainer VariableHeaderFields;
        public DataContainer Payload;

        public void AddVariableHeader(Data header)
        {
            this.VariableHeaderFields.Add(header);
        }

        public void SetPayload(Data payload)
        {
            this.Payload.Set(payload);
        }

        public void AddPayload(Data payload)
        {
            this.Payload.Add(payload);
        }

        public UInt64 CalculatePacketSize()
        {
            UInt32 payloadSize = CalculatePayloadSize();

            return (UInt64)1 + (UInt64)DataEncoderHelper.CalculateRequiredBytesForVariableByteInteger(payloadSize) + (UInt64)payloadSize;
        }

        public UInt32 CalculatePayloadSize()
        {
            return this.VariableHeaderFields.CalculateByteSize() +
                   this.Payload.CalculateByteSize();
        }

        public void EncodeInto(Stream stream)
        {
            // Create a copy and modify that one instead of the original Flags
            BitField bitField = this.Flags.Clone();

            bitField.CombineWith((byte)((byte)this.Type << 4));
            bitField.EncodeInto(stream);

            // Calculate payload size
            UInt32 payloadSize = this.CalculatePayloadSize();
            
            // write everything into the stream

            DataEncoderHelper.EncodeVariableByteInteger(payloadSize, stream);

            this.VariableHeaderFields.EncodeInto(stream);
            this.Payload.EncodeInto(stream);
        }

        public override string ToString()
        {
            return $"[Packet {this.Type}, {this.Flags.ToString()}]";
        }

        public static Packet Empty() => new Packet();
    }
}
