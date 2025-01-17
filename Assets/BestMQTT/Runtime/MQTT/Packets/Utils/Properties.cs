using System;
using System.Collections.Generic;
using System.IO;

namespace BestMQTT.Packets.Utils
{
    public struct Property
    {
        public PacketProperties Type;
        public Data Data;

        internal void EncodeInto(Stream stream)
        {
            DataEncoderHelper.EncodeVariableByteInteger((uint)this.Type, stream);
            this.Data.EncodeInto(stream);
        }

        internal UInt32 CalculateByteSize() => DataEncoderHelper.CalculateRequiredBytesForVariableByteInteger((uint)this.Type) + this.Data.CalculateByteSize();

        public override string ToString() => $"[{this.Type}: {this.Data}]";
    }

    public struct Properties
    {
        private List<Property> properties;

        internal int Count { get => this.properties?.Count ?? 0; }

        public bool IsPresent(PacketProperties property)
        {
            for (int i = 0; i < this.properties?.Count; ++i)
                if (this.properties[i].Type == property)
                    return true;

            return false;
        }

        public void ThrowIfPresent(PacketProperties property)
        {
            if (IsPresent(property))
                throw new ProtocolErrorException($"Property '{property}' already added!");
        }

        public void AddProperty(Property property)
        {
            if (this.properties == null)
                this.properties = new List<Property>();

            this.properties.Add(property);
        }

        public void AddProperty(PacketProperties type, Data data)
        {
            AddProperty(new Property { Type = type, Data = data });
        }

        public void RemoveProperty(PacketProperties type)
        {
            this.properties?.RemoveAll(p => p.Type == type);
        }

        public UInt32 CalculateByteSize(bool withSize)
        {
            if (this.properties != null)
            {
                UInt32 size = 0;
                for (int i = 0; i < this.properties.Count; ++i)
                    size += this.properties[i].CalculateByteSize();

                if (withSize)
                    size += DataEncoderHelper.CalculateRequiredBytesForVariableByteInteger(size);

                return size;
            }

            return DataEncoderHelper.CalculateRequiredBytesForVariableByteInteger(0);
        }

        public void EncodeInto(Stream stream)
        {
            if (this.properties != null)
            {
                uint size = this.CalculateByteSize(false);
                DataEncoderHelper.EncodeVariableByteInteger(size, stream);

                for (int i = 0; i < this.properties.Count; ++i)
                    this.properties[i].EncodeInto(stream);
            }
            else
                DataEncoderHelper.EncodeVariableByteInteger(0, stream);
        }

        public Property Find(PacketProperties property)
        {
            for (int i = 0; i < this.properties?.Count; i++)
                if (this.properties[i].Type == property)
                    return this.properties[i];

            return new Property();
        }

        public bool TryFindData(PacketProperties property, DataTypes dataType, out Data data)
        {
            for (int i = 0; i < this.properties?.Count; i++) {
                var prop = this.properties[i];
                if (prop.Type == property && prop.Data.IsSet && prop.Data.Type == dataType)
                {
                    data = prop.Data;
                    return true;
                }
            }

            data = Data.Empty();

            return false;
        }

        public void ForEach(PacketProperties property, Action<Data> callback)
        {
            for (int i = 0; i < this.properties?.Count; i++)
            {
                var prop = this.properties[i];
                if (prop.Type == property)
                {
                    callback(prop.Data);
                }
            }
        }

        public List<T> ConvertAll<T>(PacketProperties property, Func<Data, T> callback)
        {
            List<T> result = null;
            for (int i = 0; i < this.properties?.Count; i++)
            {
                var prop = this.properties[i];
                if (prop.Type == property)
                {
                    if (result == null)
                        result = new List<T>();
                    result.Add(callback(prop.Data));
                }
            }

            return result;
        }

        public override string ToString()
        {
            return $"{this.properties?.Count} ({CalculateByteSize(true):N0} bytes)";
        }
    }
}
