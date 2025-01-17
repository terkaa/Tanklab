using System;
using System.Collections.Generic;

namespace BestMQTT.Databases.MetadataIndexFinders
{
    public interface IEmptyMetadataIndexFinder<MetadataType> where MetadataType : Metadata
    {
        int FindFreeIndex(List<MetadataType> metadatas);
    }
}
