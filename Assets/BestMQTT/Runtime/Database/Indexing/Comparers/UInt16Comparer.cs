using System;
using System.Collections.Generic;

namespace BestMQTT.Databases.Indexing.Comparers
{
    public sealed class UInt16Comparer : IComparer<UInt16>
    {
        public int Compare(ushort x, ushort y)
        {
            return x.CompareTo(y);
        }
    }
}
