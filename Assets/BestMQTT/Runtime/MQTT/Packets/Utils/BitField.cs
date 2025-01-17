using System;
using System.IO;

namespace BestMQTT.Packets.Utils
{
    // Bits: | 7 | 6 | 5 | 4 | 3 | 2 | 1 | 0 |
    public struct BitField
    {
        private byte Bits;

        public bool this[int idx]
        {
            get => IsSet(idx);
            set => Set(idx, value);
        }

        public BitField(byte flags)
        {
            this.Bits = flags;
        }

        public void SetToZero() => this.Bits = 0;
        public void CombineWith(byte value) => this.Bits |= value;

        public bool IsSet(int idx)
        {
            return (this.Bits & (1 << idx)) != 0;
        }

        public void Set(int idx, bool value)
        {
            if (value) // set
                this.Bits |= (byte)(1 << idx);
            else // reset
                this.Bits &= (byte)~(1 << idx);
        }

        public byte Range(byte msIdx, byte lsIdx)
        {
            byte mask = 0;

            for (int idx = msIdx; idx >= lsIdx; idx--)
                mask |= (byte)(1 << idx);

            return (byte)((this.Bits & mask) >> lsIdx);
        }

        public void ClearRange(byte msIdx, byte lsIdx)
        {
            byte mask = 0;
            for (int idx = msIdx; idx >= lsIdx; idx--)
                mask |= (byte)(1 << idx);

            this.Bits ^= (byte)(this.Bits & mask);
        }

        public Data AsData() => Data.FromByte(this.Bits);
        public void EncodeInto(Stream stream) => stream.WriteByte(this.Bits);

        public BitField Clone() => new BitField(this.Bits);

        public override string ToString()
        {
            string result = "[";
            for (int idx = 7; idx >= 0; idx--)
                result += IsSet(idx) ? "X" : "-";
            return result + "]";
        }
    }
}
