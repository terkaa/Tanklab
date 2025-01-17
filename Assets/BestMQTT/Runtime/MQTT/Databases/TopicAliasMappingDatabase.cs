using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BestHTTP.PlatformSupport.Memory;
using BestHTTP.PlatformSupport.Threading;

using BestMQTT.Databases.Indexing;
using BestMQTT.Databases.Indexing.Comparers;
using BestMQTT.Databases.MetadataIndexFinders;
using BestMQTT.Databases.Utils;

namespace BestMQTT.Databases
{
    public sealed class TopicAliasMappingDatabaseOptions : DatabaseOptions
    {
        public TopicAliasMappingDatabaseOptions(string dbName) : base(dbName)
        {
            base.UseHashFile = false;
        }
    }

    public sealed class TopicAliasMappingMetadata : Metadata
    {
        internal UInt32 hash;
        internal UInt16 alias;
        internal bool sentToServer;

        public override void SaveTo(Stream stream)
        {
            base.SaveTo(stream);

            StreamUtil.EncodeVariableByteInteger(this.hash, stream);
            StreamUtil.EncodeVariableByteInteger(this.alias, stream);
        }

        public override void LoadFrom(Stream stream)
        {
            base.LoadFrom(stream);

            this.hash = StreamUtil.DecodeVariableByteInteger(stream);
            this.alias = (UInt16)StreamUtil.DecodeVariableByteInteger(stream);
        }
    }

    public sealed class TopicAliasMappingIndexingService : IndexingService<string, TopicAliasMappingMetadata>
    {
        private AVLTree<UInt32, int> index_Hash = new AVLTree<UInt32, int>(new UInt32Comparer());
        private AVLTree<UInt16, int> index_Alias = new AVLTree<UInt16, int>(new UInt16Comparer());

        public override void Index(TopicAliasMappingMetadata metadata)
        {
            base.Index(metadata);

            this.index_Hash.Add(metadata.hash, metadata.Index);
            this.index_Alias.Add(metadata.alias, metadata.Index);
        }

        public override void Remove(TopicAliasMappingMetadata metadata)
        {
            base.Remove(metadata);

            this.index_Hash.Remove(metadata.hash);
            this.index_Alias.Remove(metadata.alias);
        }

        public override void Clear()
        {
            base.Clear();

            this.index_Hash.Clear();
            this.index_Alias.Clear();
        }

        public bool ContainsHash(UInt32 hash) => this.index_Hash.ContainsKey(hash);
        public bool ContainsAlias(UInt16 alias) => this.index_Alias.ContainsKey(alias);

        public List<int> FindByHash(UInt32 hash) => this.index_Hash.Find(hash);
        public List<int> FindByAlias(UInt16 alias) => this.index_Alias.Find(alias);

        public int Count() => this.index_Hash.ElemCount;
    }

    public sealed class TopicAliasMappingMetadataService : MetadataService<TopicAliasMappingMetadata, string>
    {
        public TopicAliasMappingMetadataService(IndexingService<string, TopicAliasMappingMetadata> indexingService)
            : base(indexingService, new FindDeletedMetadataIndexFinder<TopicAliasMappingMetadata>())
        {
        }

        public TopicAliasMappingMetadata Create(string hash, UInt16 alias, int filePos, int length)
        {
            var result = base.CreateDefault(hash, MetadataFlags.None, filePos, length, (content, metadata) => {
                metadata.hash = MurmurHash2.Hash(content);
                metadata.alias = alias;
            });

            return result;
        }
    }

    public sealed class TopicAliasMappingDiskContentParser : IDiskContentParser<string>
    {
        public void Encode(Stream stream, string content)
        {
            StreamUtil.WriteLengthPrefixedString(stream, content);
        }

        public string Parse(Stream stream, int length)
        {
            return StreamUtil.ReadLengthPrefixedString(stream);
        }
    }

    public sealed class TopicAliasMappingDatabase : Database<string, TopicAliasMappingMetadata, TopicAliasMappingIndexingService, TopicAliasMappingMetadataService>
    {
        public TopicAliasMappingDatabase(string directory, string dbName, TopicAliasMappingIndexingService indexingService)
            : base(directory, new TopicAliasMappingDatabaseOptions(dbName), indexingService, new TopicAliasMappingDiskContentParser(), new TopicAliasMappingMetadataService(indexingService))
        {
        }

        public void Add(string topicName, UInt16 topicAliasMaximum)
        {
            using (new WriteLock(this.rwlock))
            {
                // find free alias
                UInt16 alias = 0;
                for (UInt16 i = 1; i <= topicAliasMaximum; i++)
                {
                    if (!this.IndexingService.ContainsAlias(i))
                    {
                        alias = i;
                        break;
                    }
                }

                if (alias == 0)
                    throw new Exception("Couldn't found free alias!");

                Add(alias, topicName);
            }
        }

        private void Add(UInt16 alias, string topicName)
        {
            var (pos, length) = this.DiskManager.Append(topicName);
            this.MetadataService.Create(topicName, alias, pos, length);

            this.FlagDirty();
        }

        public void Set(UInt16 alias, string topicName)
        {
            using (new WriteLock(this.rwlock))
            {
                var metadataIndexes = this.IndexingService.FindByAlias(alias);
                if (metadataIndexes == null)
                {
                    Add(alias, topicName);
                    return;
                }

                this.Delete(metadataIndexes);

                Add(alias, topicName);
            }
        }

        public string Find(UInt16 alias)
        {
            if (alias == 0)
                return null;

            using (new ReadLock(this.rwlock))
            {
                var metadataIndexes = this.IndexingService.FindByAlias(alias);
                if (metadataIndexes == null)
                    return null;

                return this.FromMetadataIndexes(metadataIndexes).FirstOrDefault();
            }
        }

        public (UInt16 alias, bool sentToServer) Find(string topicName)
        {
            if (string.IsNullOrEmpty(topicName))
                return (0, false);

            using (new ReadLock(this.rwlock))
            {
                if (this.IndexingService.Count() == 0)
                    return (0, false);

                var hash = MurmurHash2.Hash(topicName);

                var metadataIndexes = this.IndexingService.FindByHash(hash);
                if (metadataIndexes == null)
                    return (0, false);

                var metadata = this.MetadataService.Metadatas[metadataIndexes[0]];
                return (metadata.alias, metadata.sentToServer);
            }
        }

        public UInt16 Count()
        {
            using (new ReadLock(this.rwlock))
                return (UInt16)this.IndexingService.Count();
        }

        internal void SetSent(ushort alias, bool sentToServer)
        {
            using (new WriteLock(this.rwlock))
            {
                var metadataIndexes = this.IndexingService.FindByAlias(alias);
                if (metadataIndexes == null)
                    return;

                this.MetadataService.Metadatas[metadataIndexes[0]].sentToServer = sentToServer;

                this.FlagDirty();
            }
        }
    }

    // https://github.com/jitbit/MurmurHash.net
    public class MurmurHash2
    {
        const uint m = 0x5bd1e995;
        const int r = 24;

        public static uint Hash(string str)
        {
            var count = System.Text.Encoding.UTF8.GetByteCount(str);
            var buffer = BufferPool.Get(count, true);

            System.Text.Encoding.UTF8.GetBytes(str, 0, str.Length, buffer, 0);

            var hash = Hash(buffer, count);

            BufferPool.Release(buffer);

            return hash;
        }

        private static uint Hash(byte[] data, int dataLength, uint seed = 0xc58f1a7a)
        {
            int length = dataLength;
            if (length == 0)
                return 0;
            uint h = seed ^ (uint)length;
            int currentIndex = 0;
            while (length >= 4)
            {
                uint k = (uint)(data[currentIndex++] | data[currentIndex++] << 8 | data[currentIndex++] << 16 | data[currentIndex++] << 24);
                k *= m;
                k ^= k >> r;
                k *= m;

                h *= m;
                h ^= k;
                length -= 4;
            }
            switch (length)
            {
                case 3:
                    h ^= (UInt16)(data[currentIndex++] | data[currentIndex++] << 8);
                    h ^= (uint)(data[currentIndex] << 16);
                    h *= m;
                    break;
                case 2:
                    h ^= (UInt16)(data[currentIndex++] | data[currentIndex] << 8);
                    h *= m;
                    break;
                case 1:
                    h ^= data[currentIndex];
                    h *= m;
                    break;
                default:
                    break;
            }

            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;

            return h;
        }
    }
}
