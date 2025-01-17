using System;
using System.Collections.Generic;

namespace BestMQTT.Databases.Indexing.Comparers
{
    public sealed class UInt32Comparer : IComparer<UInt32>
    {
        public int Compare(UInt32 x, UInt32 y)
        {
            return x.CompareTo(y);
        }
    }
}
