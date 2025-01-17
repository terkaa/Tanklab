using System;
using System.IO;
using System.Text;

using BestHTTP.PlatformSupport.Memory;

namespace BestMQTT.Databases.Utils
{
    public static class StreamUtil
    {
        public static void WriteLengthPrefixedString(this Stream stream, string str)
        {
            if (str != null)
            {
                var byteCount = Encoding.UTF8.GetByteCount(str);

                if (byteCount >= 1 << 16)
                    throw new ArgumentException($"byteCount({byteCount})");

                stream.WriteByte((byte)(byteCount >> 8));
                stream.WriteByte((byte)(byteCount));

                byte[] tmp = BufferPool.Get(byteCount, true);

                Encoding.UTF8.GetBytes(str, 0, str.Length, tmp, 0);
                stream.Write(tmp, 0, byteCount);

                BufferPool.Release(tmp);
            }
            else
            {
                stream.WriteByte(0);
                stream.WriteByte(0);
            }
        }

        public static string ReadLengthPrefixedString(this Stream stream)
        {
            int strLength = stream.ReadByte() << 8 | stream.ReadByte();
            string result = null;

            if (strLength != 0)
            {
                byte[] buffer = BufferPool.Get(strLength, true);

                stream.Read(buffer, 0, strLength);
                result = System.Text.Encoding.UTF8.GetString(buffer, 0, strLength);

                BufferPool.Release(buffer);
            }

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

                if (multiplier > 128 * 128 * 128 && (encodedByte & 128) != 0)
                    throw new Exception("Malformed Variable Byte Integer");

                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            return value;
        }
    }
}
