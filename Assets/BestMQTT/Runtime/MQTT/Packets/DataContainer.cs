using System;
using System.Collections.Generic;
using System.IO;

using BestMQTT.Packets.Utils;

namespace BestMQTT.Packets
{
    public struct DataContainer
    {
        public int Count { get => this._fields != null ? this._fields.Count : this._field.IsSet ? 1 : 0; }
        public Properties Properties { get => this._properties.Properties; }

        private Data _field;
        private List<Data> _fields;
        private Data _properties;

        public Data this[int idx]
        {
            get => this._fields != null ? this._fields[idx] : this._field;
            set
            {
                if (this._fields != null)
                    this._fields[idx] = value;
                else
                    this._field = value;
            }
        }

        public void Add(Data data)
        {
            if (this._fields != null)
                this._fields.Add(data);
            else if (this._field.IsSet)
            {
                if (this._fields == null)
                    this._fields = new List<Data>();

                this._fields.Add(this._field);
                this._fields.Add(data);

                this._field = Data.Empty();
            }
            else
                this._field = data;

            if (data.Type == DataTypes.Property)
                this._properties = data;
        }

        public void Set(Data data)
        {
            this._fields?.Clear();
            this._fields = null;
            this._field = data;

            if (data.Type == DataTypes.Property)
                this._properties = data;
        }

        public UInt32 CalculateByteSize()
        {
            if (this._field.IsSet)
                return this._field.CalculateByteSize();
            else if (this._fields != null)
            {
                UInt32 size = 0;
                for (int i = 0; i < this._fields.Count; ++i)
                    size += this._fields[i].CalculateByteSize();
                return size;
            }

            return 0;
        }

        public void EncodeInto(Stream stream)
        {
            if (this._field.IsSet)
                this._field.EncodeInto(stream);
            else if (this._fields != null)
            {
                for (int i = 0; i < this._fields.Count; ++i)
                    this._fields[i].EncodeInto(stream);
            }
        }
    }
}
