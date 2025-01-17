using System.Collections.Generic;

namespace BestMQTT.Databases.Indexing.Comparers
{
    public sealed class StringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return x.CompareTo(y);
        }
    }
}
