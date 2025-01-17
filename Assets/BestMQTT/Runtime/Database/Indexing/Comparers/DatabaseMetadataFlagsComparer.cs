using System.Collections.Generic;

namespace BestMQTT.Databases.Indexing.Comparers
{
    public sealed class DatabaseMetadataFlagsComparer : IComparer<MetadataFlags>
    {
        public int Compare(MetadataFlags x, MetadataFlags y)
        {
            return ((byte)x).CompareTo((byte)y);
        }
    }
}
