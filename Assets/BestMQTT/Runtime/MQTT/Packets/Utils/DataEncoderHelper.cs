using System;
using System.Collections.Generic;
using System.IO;

using BestHTTP.Extensions;
using BestHTTP.PlatformSupport.Memory;

namespace BestMQTT.Packets.Utils
{
    public static class DataEncoderHelper
    {
        public static void EncodeTwoByteInteger(UInt16 value, Stream encodeTo)
        {
            encodeTo.WriteByte((byte)(value >> 8));
            encodeTo.WriteByte((byte)(value));
        }

        public static UInt16 DecodeTwoByteInteger(Stream decodeFrom)
        {
            int msb = decodeFrom.ReadByte();
            int lsb = decodeFrom.ReadByte();

            return (UInt16)((msb << 8) + lsb);
        }

        public static void EncodeFourByteInteger(UInt32 value, Stream encodeTo)
        {
            encodeTo.WriteByte((byte)(value >> 24));
            encodeTo.WriteByte((byte)(value >> 16));
            encodeTo.WriteByte((byte)(value >> 8));
            encodeTo.WriteByte((byte)(value & 0xFF));
        }

        public static UInt32 DecodeFourByteInteger(Stream decodeFrom)
        {
            int b4 = decodeFrom.ReadByte();
            int b3 = decodeFrom.ReadByte();
            int b2 = decodeFrom.ReadByte();
            int b1 = decodeFrom.ReadByte();

            return (UInt32)(b4 << 24 |
                            b3 << 16 |
                            b2 << 8 |
                            b1);
        }

        public static void EncodeUTF8String(string value, Stream encodeTo)
        {
            if (value == null)
                value = string.Empty;

            int encodedLength = System.Text.Encoding.UTF8.GetByteCount(value);
            if (encodedLength > UInt16.MaxValue)
                throw new Exception($"String too long {encodedLength}");

            EncodeTwoByteInteger((UInt16)encodedLength, encodeTo);

            var buffer = BufferPool.Get(encodedLength, true);
            System.Text.Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);

            encodeTo.Write(buffer, 0, encodedLength);

#if UNITY_EDITOR
            Array.Clear(buffer, 0, encodedLength);
#endif

            BufferPool.Release(buffer);
        }

        public static uint CalculateByteSize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 2;

            return (uint)(2 + System.Text.Encoding.UTF8.GetByteCount(value));
        }

        public static string DecodeUTF8String(Stream decodeFrom)
        {
            int byteCount = DecodeTwoByteInteger(decodeFrom);

            var buffer = BufferPool.Get(byteCount, true);

            int read = decodeFrom.Read(buffer, 0, byteCount);
            if (read < byteCount)
                throw new Exception($"Not enough data in {nameof(DecodeUTF8String)}!");

            var result = System.Text.Encoding.UTF8.GetString(buffer, 0, byteCount);

            BufferPool.Release(buffer);

            return result;
        }

        public static void EncodeVariableByteInteger(UInt32 value, Stream encodeTo)
        {
            byte encodedByte;
            do
            {
                encodedByte = (byte)(value % 128);
                value /= 128;
                // if there are more data to encode, set the top bit of this byte
                if (value > 0)
                    encodedByte = (byte)(encodedByte | 128);

                encodeTo.WriteByte(encodedByte);
            }
            while (value > 0);
        }

        public static UInt32 DecodeVariableByteInteger(Stream decodeFrom)
        {
            int multiplier = 1;
            UInt32 value = 0;
            byte encodedByte = 0;
            do
            {
                encodedByte = (byte)decodeFrom.ReadByte();

                value += (UInt32)((encodedByte & 127) * multiplier);
                if (multiplier > 128 * 128 * 128)
                    throw new Exception("Malformed Variable Byte Integer");
                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            return value;
        }

        public static byte CalculateRequiredBytesForVariableByteInteger(UInt32 value)
        {
            byte size = 0;
            byte encodedByte;
            do
            {
                encodedByte = (byte)(value % 128);
                value /= 128;
                // if there are more data to encode, set the top bit of this byte
                if (value > 0)
                    encodedByte = (byte)(encodedByte | 128);

                size++;
            }
            while (value > 0);

            return size;
        }

        public static (bool success, UInt32 integer) TryToDecodeVariableByteInteger(PeekableIncomingSegmentStream stream)
        {
            int multiplier = 1;
            UInt32 value = 0;
            byte encodedByte = 0;
            do
            {
                int peeked_byte = stream.PeekByte();
                if (peeked_byte < 0)
                    return (false, 0);

                encodedByte = (byte)peeked_byte;

                value += (UInt32)((encodedByte & 127) * multiplier);
                if (multiplier > 128 * 128 * 128)
                    throw new Exception("Malformed Variable Byte Integer");
                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            return (true, value);
        }

        public static void EncodeBinary(BufferSegment binary, Stream encodeTo)
        {
            if (binary.Count > UInt16.MaxValue)
                throw new Exception($"Binary too large {binary.Count}");

            EncodeTwoByteInteger((UInt16)binary.Count, encodeTo);
            if (binary.Data != null)
                encodeTo.Write(binary.Data, binary.Offset, binary.Count);
        }

        public static BufferSegment DecodeBinary(Stream decodeFrom)
        {
            int length = DecodeTwoByteInteger(decodeFrom);

            var buffer = BufferPool.Get(length, true);
            decodeFrom.Read(buffer, 0, length);

            return new BufferSegment(buffer, 0, length);
        }

        public static void EncodeUTF8StringPair(KeyValuePair<string, string> kvp, Stream encodeTo)
        {
            EncodeUTF8String(kvp.Key, encodeTo);
            EncodeUTF8String(kvp.Value, encodeTo);
        }

        public static KeyValuePair<string, string> DecodeUTF8StringPair(Stream decodeFrom)
        {
            string key = DecodeUTF8String(decodeFrom);
            string value = DecodeUTF8String(decodeFrom);

            return new KeyValuePair<string, string>(key, value);
        }
    }
}
