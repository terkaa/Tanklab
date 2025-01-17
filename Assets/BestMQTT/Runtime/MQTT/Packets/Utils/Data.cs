using System;
using System.Collections.Generic;
using System.IO;

using BestHTTP.PlatformSupport.Memory;

namespace BestMQTT.Packets.Utils
{
    public enum DataTypes
    {
        UnSet,

        Bits,

        Bool,

        Byte,
        TwoByteInteger,
        FourByteInteger,
        VariableByteInteger,

        UTF8String,
        UTF8StringPair,

        Binary,

        Property,

        Raw
    }

    public struct Data
    {
        public bool IsSet => this.Type != DataTypes.UnSet;

        public DataTypes Type;

        public BitField Bits;
        public UInt32 Integer;
        public KeyValuePair<string, string> UTF8String;
        public BufferSegment Binary;
        public Properties Properties;

        public static Data Empty() => new Data { Type = DataTypes.UnSet };

        public static Data FromBitField(BitField bitField) => new Data { Type = DataTypes.Bits, Bits = bitField };
        public static Data FromBool(bool value) => new Data { Type = DataTypes.Bool, Integer = value ? (uint)1 : (uint)0 };
        public static Data FromByte(int value) => new Data { Type = DataTypes.Byte, Integer = (byte)value };
        public static Data FromTwoByteInteger(uint value) => new Data { Type = DataTypes.TwoByteInteger, Integer = value };
        public static Data FromFourByteInteger(uint value) => new Data { Type = DataTypes.FourByteInteger, Integer = value };
        public static Data FromVariableByteInteger(uint value) => new Data { Type = DataTypes.VariableByteInteger, Integer = value };

        public static Data FromString(string value) => new Data { Type = DataTypes.UTF8String, UTF8String = new KeyValuePair<string, string>(value, null) };
        public static Data FromStringPair(string key, string value) => new Data { Type = DataTypes.UTF8StringPair, UTF8String = new KeyValuePair<string, string>(key, value) };

        public static Data FromArray(byte[] buffer) => new Data { Type = DataTypes.Binary, Binary = buffer != null ? new BufferSegment(buffer, 0, buffer.Length) : BufferSegment.Empty };
        public static Data FromBuffer(BufferSegment buffer) => new Data { Type = DataTypes.Binary, Binary = buffer };
        public static Data FromRaw(byte[] buffer) => new Data { Type = DataTypes.Raw, Binary = buffer != null ? new BufferSegment(buffer, 0, buffer.Length) : BufferSegment.Empty };
        public static Data FromRaw(BufferSegment buffer) => new Data { Type = DataTypes.Raw, Binary = buffer };

        public static Data FromProperties(Properties properties) => new Data { Type = DataTypes.Property, Properties = properties };

        public static implicit operator BitField(Data d) => d.Bits;
        public static implicit operator bool(Data d) => d.Integer != 0;

        public static implicit operator byte(Data d) => (byte)d.Integer;
        public static implicit operator UInt16(Data d) => (UInt16)d.Integer;
        public static implicit operator UInt32(Data d) => d.Integer;

        public static implicit operator string(Data d) => d.UTF8String.Key;
        public static implicit operator KeyValuePair<string, string>(Data d) => d.UTF8String;

        public static implicit operator BufferSegment(Data d) => d.Binary;

        public void EncodeInto(Stream stream)
        {
            switch (this.Type)
            {
                default: throw new NotImplementedException($"{this.Type} in {nameof(EncodeInto)}");

                case DataTypes.Bits: stream.WriteByte((byte)this.Bits.AsData().Integer); break;
                case DataTypes.Bool:
                case DataTypes.Byte: stream.WriteByte((byte)this.Integer); break;
                case DataTypes.TwoByteInteger: DataEncoderHelper.EncodeTwoByteInteger((ushort)this.Integer, stream); break;
                case DataTypes.FourByteInteger: DataEncoderHelper.EncodeFourByteInteger(this.Integer, stream); break;
                case DataTypes.VariableByteInteger: DataEncoderHelper.EncodeVariableByteInteger(this.Integer, stream); break;

                case DataTypes.UTF8String: DataEncoderHelper.EncodeUTF8String(this.UTF8String.Key, stream); break;
                case DataTypes.UTF8StringPair: DataEncoderHelper.EncodeUTF8StringPair(this.UTF8String, stream); break;

                case DataTypes.Binary: DataEncoderHelper.EncodeBinary(this.Binary, stream); break;

                case DataTypes.Property: this.Properties.EncodeInto(stream); break;

                case DataTypes.Raw: if (this.Binary != BufferSegment.Empty) stream.Write(this.Binary.Data, this.Binary.Offset, this.Binary.Count); break;
            }
        }

        public UInt32 CalculateByteSize()
        {
            switch (this.Type)
            {
                case DataTypes.Bits:
                case DataTypes.Bool:
                case DataTypes.Byte: return 1;
                case DataTypes.TwoByteInteger: return 2;
                case DataTypes.FourByteInteger: return 4;
                case DataTypes.VariableByteInteger: return DataEncoderHelper.CalculateRequiredBytesForVariableByteInteger(this.Integer);
                case DataTypes.UTF8String: return DataEncoderHelper.CalculateByteSize(this.UTF8String.Key);
                case DataTypes.UTF8StringPair: return DataEncoderHelper.CalculateByteSize(this.UTF8String.Key) + DataEncoderHelper.CalculateByteSize(this.UTF8String.Value);
                case DataTypes.Binary: return (UInt32)(2 + this.Binary.Count);
                case DataTypes.Property: return this.Properties.CalculateByteSize(true);
                case DataTypes.Raw: return (UInt32)this.Binary.Count;
                default:
                    throw new NotImplementedException($"{this.Type} in {nameof(CalculateByteSize)}");
            }
        }

        public static Data ReadAs(DataTypes type, Stream stream, ref UInt32 remainingLength)
        {
            switch (type)
            {
                case DataTypes.Bits: remainingLength--; return Data.FromBitField(new BitField((byte)stream.ReadByte()));
                case DataTypes.Bool: remainingLength--; return Data.FromBool(stream.ReadByte() != 0);
                case DataTypes.Byte: remainingLength--; return Data.FromByte(stream.ReadByte());
                case DataTypes.TwoByteInteger: remainingLength -= 2; return Data.FromTwoByteInteger((uint)(stream.ReadByte() << 8 | stream.ReadByte()));
                case DataTypes.FourByteInteger:
                    {
                        remainingLength -= 4;
                        UInt32 value = (UInt32)(stream.ReadByte() << 24 |
                                                stream.ReadByte() << 16 |
                                                stream.ReadByte() << 8 |
                                                stream.ReadByte());

                        return Data.FromFourByteInteger(value);
                    }

                case DataTypes.VariableByteInteger:
                    {
                        var data = Data.FromVariableByteInteger(DataEncoderHelper.DecodeVariableByteInteger(stream));

                        remainingLength -= DataEncoderHelper.CalculateRequiredBytesForVariableByteInteger(data.Integer);

                        return data;
                    }
                    
                case DataTypes.UTF8String:
                    {
                        var length = ReadAs(DataTypes.TwoByteInteger, stream, ref remainingLength).Integer;

                        var buffer = BufferPool.Get(length, true);

                        stream.Read(buffer, 0, (int)length);

                        string utf8string = System.Text.Encoding.UTF8.GetString(buffer, 0, (int)length);
                        var data = Data.FromString(utf8string);

                        BufferPool.Release(buffer);

                        remainingLength -= length;

                        return data;
                    }

                case DataTypes.UTF8StringPair:
                    {
                        Data key = ReadAs(DataTypes.UTF8String, stream, ref remainingLength);
                        Data value = ReadAs(DataTypes.UTF8String, stream, ref remainingLength);

                        return Data.FromStringPair(key.UTF8String.Key, value.UTF8String.Key);
                    }

                case DataTypes.Binary:
                    {
                        var length = ReadAs(DataTypes.TwoByteInteger, stream, ref remainingLength).Integer;
                        byte[] buffer = new byte[length];

                        stream.Read(buffer, 0, (int)length);

                        remainingLength -= length;

                        return Data.FromArray(buffer);
                    }

                case DataTypes.Property:
                    {
                        var length = DataEncoderHelper.DecodeVariableByteInteger(stream);

                        remainingLength -= DataEncoderHelper.CalculateRequiredBytesForVariableByteInteger(length);

                        var properties = ReadProperties(stream, length, ref remainingLength);
                        
                        return Data.FromProperties(properties);
                    }

                case DataTypes.Raw:
                    {
                        var buffer = BufferPool.Get(remainingLength, true);
                        stream.Read(buffer, 0, (int)remainingLength);

                        var data = Data.FromRaw(new BufferSegment(buffer, 0, (int)remainingLength));

                        remainingLength = 0;

                        return data;
                    }

                default:
                    throw new NotImplementedException($"Can't read data type '{type}'!");
            }
        }

        public static Properties ReadProperties(Stream stream, UInt32 length, ref UInt32 remainingLength)
        {
            var properties = new Properties();

            while (length > 0)
            {
                //BestHTTP.HTTPManager.Logger.Information(nameof(Data), $"{nameof(ReadProperties)}({stream.Position}/{stream.Length}, {length}, {remainingLength})");

                UInt32 remainingLengthBefore = remainingLength;

                PacketProperties type = (PacketProperties)stream.ReadByte();
                remainingLength--;
                
                switch (type)
                {
                    // Byte
                    case PacketProperties.PayloadFormatIndicator:
                    case PacketProperties.RequestProblemInformation:
                    case PacketProperties.RequestResponseInformation:
                    case PacketProperties.MaximumQoS:
                    case PacketProperties.RetainAvailable:
                    case PacketProperties.WildcardSubscriptionAvailable:
                    case PacketProperties.SubscriptionIdentifierAvailable:
                    case PacketProperties.SharedSubscriptionAvailable:
                        properties.AddProperty(new Property { Type = type, Data = ReadAs(DataTypes.Byte, stream, ref remainingLength) });
                        break;

                    // Four Byte Integer
                    case PacketProperties.MessageExpiryInterval:
                    case PacketProperties.SessionExpiryInterval:
                    case PacketProperties.WillDelayInterval:
                    case PacketProperties.MaximumPacketSize:
                        properties.AddProperty(new Property { Type = type, Data = ReadAs(DataTypes.FourByteInteger, stream, ref remainingLength) });
                        break;

                    // UTF-8 Encoded String
                    case PacketProperties.ContentType:
                    case PacketProperties.ResponseTopic:
                    case PacketProperties.AssignedClientIdentifier:
                    case PacketProperties.AuthenticationMethod:
                    case PacketProperties.ResponseInformation:
                    case PacketProperties.ServerReference:
                    case PacketProperties.ReasonString:
                        properties.AddProperty(new Property { Type = type, Data = ReadAs(DataTypes.UTF8String, stream, ref remainingLength) });
                        break;

                    // Binary Data
                    case PacketProperties.CorrelationData:
                    case PacketProperties.AuthenticationData:
                        properties.AddProperty(new Property { Type = type, Data = ReadAs(DataTypes.Binary, stream, ref remainingLength) });
                        break;

                    // Variable Byte Integer
                    case PacketProperties.SubscriptionIdentifier:
                        properties.AddProperty(new Property { Type = type, Data = ReadAs(DataTypes.VariableByteInteger, stream, ref remainingLength) });
                        break;

                    // Two Byte Integer
                    case PacketProperties.ServerKeepAlive:
                    case PacketProperties.ReceiveMaximum:
                    case PacketProperties.TopicAliasMaximum:
                    case PacketProperties.TopicAlias:
                        properties.AddProperty(new Property { Type = type, Data = ReadAs(DataTypes.TwoByteInteger, stream, ref remainingLength) });
                        break;

                    // UTF-8 String Pair
                    case PacketProperties.UserProperty:
                        properties.AddProperty(new Property { Type = type, Data = ReadAs(DataTypes.UTF8StringPair, stream, ref remainingLength) });
                        break;
                }

                //BestHTTP.HTTPManager.Logger.Information(nameof(Data), $"{nameof(ReadProperties)} {remainingLengthBefore} - {remainingLength} = {remainingLengthBefore - remainingLength} => {length} - {remainingLengthBefore - remainingLength} => {length - remainingLengthBefore - remainingLength}");

                length -= remainingLengthBefore - remainingLength;
            }

            return properties;
        }

        public override string ToString()
        {
            string result = $"[{this.Type} ";
            switch(this.Type)
            {
                case DataTypes.Bits: result += this.Bits.ToString(); break;
                case DataTypes.Byte:
                case DataTypes.TwoByteInteger:
                case DataTypes.FourByteInteger:
                case DataTypes.VariableByteInteger:
                    result += this.Integer;
                    break;

                case DataTypes.UTF8String:
                    result += this.UTF8String.Key;
                    break;

                case DataTypes.UTF8StringPair:
                    result += $"Key: '{this.UTF8String.Key}', Value: '{this.UTF8String.Value}'";
                    break;

                case DataTypes.Binary:
                case DataTypes.Raw:
                    result += $"Offset: {this.Binary.Offset}, Count: {this.Binary.Count}";
                    break;

                case DataTypes.Property:
                    result += this.Properties.ToString();
                    break;
            }

            return result + "]";
        }
    }
}
