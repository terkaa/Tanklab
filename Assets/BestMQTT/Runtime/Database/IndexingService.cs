using System;
using System.Collections.Generic;

using BestMQTT.Databases.Indexing;
using BestMQTT.Databases.Indexing.Comparers;

namespace BestMQTT.Databases
{
    public abstract class IndexingService<ContentType, MetadataType> where MetadataType : Metadata
    {
        public AVLTree<MetadataFlags, int> index_Flags = new AVLTree<MetadataFlags, int>(new DatabaseMetadataFlagsComparer());

        public virtual void Remove(MetadataType metadata)
        {
            if (metadata.Flags == MetadataFlags.None)
            {
                this.index_Flags.Remove(MetadataFlags.None, metadata.Index);
                return;
            }

            byte flags = (byte)metadata.Flags;
            for (int i = 0; i < 8; i++)
            {
                byte mask = (byte)(1 << i);
                byte masked = (byte)(flags & mask);
                if (masked != 0)
                    this.index_Flags.Add((MetadataFlags)masked, metadata.Index);
            }
        }

        public virtual List<int> FindByFlags(MetadataFlags flags)
        {
            List<int> result = new List<int>();

            byte bFlags = (byte)flags;
            for (int i = 0; i < 8; i++)
            {
                byte mask = (byte)(1 << i);
                byte masked = (byte)(bFlags & mask);
                if (masked != 0)
                {
                    var indexes = this.index_Flags.Find((MetadataFlags)masked);
                    if (indexes != null)
                        result.AddRange(indexes);
                }
            }

            return result;
        }

        public virtual void Index(MetadataType metadata)
        {
            IndexByFlags(metadata);
        }

        public virtual void Clear()
        {
            this.index_Flags.Clear();
        }

        protected void IndexByFlags(MetadataType metadata)
        {
            if (metadata.Flags == MetadataFlags.None)
            {
                this.index_Flags.Add(MetadataFlags.None, metadata.Index);
                return;
            }

            byte flags = (byte)metadata.Flags;
            for (int i = 0; i < 8; i++)
            {
                byte mask = (byte)(1 << i);
                byte masked = (byte)(flags & mask);
                if (masked != 0)
                    this.index_Flags.Add((MetadataFlags)masked, metadata.Index);
            }
        }
    }
}
