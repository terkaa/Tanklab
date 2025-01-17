using System;
using System.IO;

namespace BestMQTT.Databases
{
    [Flags]
    public enum MetadataFlags : byte
    {
        None        = 0x01,
        UserAdded   = 0x02,
        Locked      = 0x04
    }

    public abstract class Metadata
    {
        public int Index;
        public int FilePosition;
        public int Length;

        public bool IsDeleted => this.FilePosition == -1 && this.Length == -1;

        public MetadataFlags Flags;

        public bool IsUserAdded { get { return HasFlag(MetadataFlags.UserAdded); } set { SetFlag(MetadataFlags.UserAdded, value); } }
        public bool IsLocked { get { return HasFlag(MetadataFlags.Locked); } set { SetFlag(MetadataFlags.Locked, value); } }

        public void MarkForDelete()
        {
            this.FilePosition = -1;
            this.Length = -1;
        }

        public bool HasFlag(MetadataFlags flag) => (this.Flags & flag) == flag;
        public void SetFlag(MetadataFlags flag, bool on) => this.Flags = (this.Flags ^ flag) & (MetadataFlags)((on ? 1 : 0) * (byte)flag);

        public virtual void SaveTo(Stream stream)
        {
            Utils.StreamUtil.EncodeVariableByteInteger((uint)this.FilePosition, stream);
            Utils.StreamUtil.EncodeVariableByteInteger((uint)this.Length, stream);

            stream.WriteByte((byte)(this.Flags));
        }

        public virtual void LoadFrom(Stream stream)
        {
            this.FilePosition = (int)Utils.StreamUtil.DecodeVariableByteInteger(stream);
            this.Length = (int)Utils.StreamUtil.DecodeVariableByteInteger(stream);

            this.Flags = (MetadataFlags)stream.ReadByte();
        }

        public override string ToString()
        {
            return $"[Metadata Idx: {Index}, Pos: {FilePosition}, Length: {Length}, Flags: {Flags}, IsDeleted: {IsDeleted}]";
        }
    }
}
