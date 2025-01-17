using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BestHTTP.PlatformSupport.Threading;

using BestMQTT.Databases.Indexing;
using BestMQTT.Databases.Indexing.Comparers;
using BestMQTT.Databases.MetadataIndexFinders;
using BestMQTT.Databases.Utils;
using BestMQTT.Packets;
using BestMQTT.Packets.Utils;

namespace BestMQTT.Databases
{
    internal sealed class OutgoingPacketDatabaseOptions : DatabaseOptions
    {
        public OutgoingPacketDatabaseOptions(string dbName)
            : base(dbName)
        {
        }
    }

    internal sealed class OutgoingPacketMetadata : Metadata
    {
        internal UInt16 packetId;

        public override void SaveTo(Stream stream)
        {
            base.SaveTo(stream);

            StreamUtil.EncodeVariableByteInteger(this.packetId, stream);
        }

        public override void LoadFrom(Stream stream)
        {
            base.LoadFrom(stream);

            this.packetId = (UInt16)StreamUtil.DecodeVariableByteInteger(stream);
        }
    }

    internal sealed class OutgoingPacketMetadataService : MetadataService<OutgoingPacketMetadata, Packet>
    {
        public OutgoingPacketMetadataService(IndexingService<Packet, OutgoingPacketMetadata> indexingService, IEmptyMetadataIndexFinder<OutgoingPacketMetadata> emptyMetadataIndexFinder) : base(indexingService, emptyMetadataIndexFinder)
        {
        }

        public OutgoingPacketMetadata Create(UInt16 packetId, in Packet value, int filePos, int length)
        {
            var result = base.CreateDefault(value, MetadataFlags.None, filePos, length, (content, metadata) => {
                metadata.packetId = packetId;
            });

            return result;
        }
    }

    internal sealed class OutgoingPacketIndexingService : IndexingService<Packet, OutgoingPacketMetadata>
    {
        private AVLTree<UInt16, int> index_packetId = new AVLTree<ushort, int>(new UInt16Comparer());

        public override void Index(OutgoingPacketMetadata metadata)
        {
            base.Index(metadata);

            this.index_packetId.Add(metadata.packetId, metadata.Index);
        }

        public override void Remove(OutgoingPacketMetadata metadata)
        {
            base.Remove(metadata);

            this.index_packetId.Remove(metadata.packetId);
        }

        public override void Clear()
        {
            base.Clear();

            this.index_packetId.Clear();
        }

        public bool ContainsKey(UInt16 key) => this.index_packetId.ContainsKey(key);
        public List<int> FindByPacketId(UInt16 packetId) => this.index_packetId.Find(packetId);
    }

    internal sealed class OutgoingPacketDiskContentParser : IDiskContentParser<Packet>
    {
        public void Encode(Stream stream, Packet content)
        {
            if (content.Type != PacketTypes.Publish)
                throw new NotImplementedException($"Storing {content.Type} packets isn't supported!");
            content.EncodeInto(stream);
        }

        public Packet Parse(Stream stream, int length)
        {
            // We know for a fact that we store only PUBLISH packets only in this database

            BitField firstByte = new BitField((byte)stream.ReadByte());
            PacketTypes type = (PacketTypes)firstByte.Range(7, 4);

            if (type != PacketTypes.Publish)
                throw new NotImplementedException($"Parsing {type} packets isn't supported!");

            firstByte.ClearRange(7, 4);

            var packet = new Packet();
            packet.Type = PacketTypes.Publish;
            packet.Flags = firstByte;

            UInt32 remainingLength = DataEncoderHelper.DecodeVariableByteInteger(stream);

            packet.AddVariableHeader(Data.ReadAs(DataTypes.UTF8String, stream, ref remainingLength));

            // The Packet Identifier field is only present in PUBLISH packets where the QoS level is 1 or 2.
            if (firstByte.Range(2, 1) > 0)
                packet.AddVariableHeader(Data.ReadAs(DataTypes.TwoByteInteger, stream, ref remainingLength));

            packet.AddVariableHeader(Data.ReadAs(DataTypes.Property, stream, ref remainingLength));

            if (remainingLength > 0)
                packet.AddPayload(Data.ReadAs(DataTypes.Raw, stream, ref remainingLength));

            return packet;
        }
    }

    // DefaultEmptyMetadataIndexFinder appends the new metadata to the end of the metadata list, this way we know the order of the packets!
    internal sealed class OutgoingPacketDatabase : Database<Packet, OutgoingPacketMetadata, OutgoingPacketIndexingService, OutgoingPacketMetadataService>
    {
        public OutgoingPacketDatabase(string directory, string dbName, OutgoingPacketIndexingService indexingService)
            : base(directory,
                  new OutgoingPacketDatabaseOptions(dbName),
                  indexingService,
                  new OutgoingPacketDiskContentParser(),
                  new OutgoingPacketMetadataService(indexingService, new DefaultEmptyMetadataIndexFinder<OutgoingPacketMetadata>()))
        {
        }

        public void Add(UInt16 packetId, in Packet packet)
        {
            using (new WriteLock(this.rwlock))
            {
                (int filePos, int length) = this.DiskManager.Append(packet);
                this.MetadataService.Create(packetId, in packet, filePos, length);

                this.FlagDirty();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packetId"></param>
        /// <returns>false if packetId not found</returns>
        public bool TryRemoveByPacketId(UInt16 packetId)
        {
            List<int> metadataIndexes = null;
            using (new ReadLock(this.rwlock))
            {
                metadataIndexes = this.IndexingService.FindByPacketId(packetId);
                if (metadataIndexes == null || metadataIndexes.Count == 0)
                    return false;
            }

            using (new WriteLock(this.rwlock))
            {
                for (int i = 0; i < metadataIndexes.Count; ++i)
                    this.DeleteMetadata(this.MetadataService.Metadatas[metadataIndexes[i]]);

                this.FlagDirty();
            }            

            return true;
        }

        public (bool found, UInt16 packetId, Packet packet) TryDequeue()
        {
            using(new WriteLock(this.rwlock))
            {
                var firstMetadata = this.MetadataService.Metadatas.FirstOrDefault(m => !m.IsDeleted);
                if (firstMetadata == null)
                    return (false, 0, new Packet());

                var packet = this.DiskManager.Load(firstMetadata);

                this.DeleteMetadata(firstMetadata);
                this.FlagDirty();

                return (true, firstMetadata.packetId, packet);
            }
        }

        public (bool found, UInt16 packetId, Packet packet) GetNext(UInt16 packetId)
        {
            using (new ReadLock(this.rwlock))
            {
                var firstMetadata = this.MetadataService.Metadatas.FirstOrDefault(m => !m.IsDeleted && m.packetId > packetId);
                if (firstMetadata == null)
                    return (false, 0, new Packet());

                var packet = this.DiskManager.Load(firstMetadata);

                return (true, firstMetadata.packetId, packet);
            }
        }

        public bool IsPacketIDInUse(UInt16 packetId)
        {
            using (new ReadLock(this.rwlock))
                return this.IndexingService.ContainsKey(packetId);
        }

        public int Count()
        {
            using (new ReadLock(this.rwlock))
                return base.IndexingService.index_Flags.ElemCount;
        }
    }
}
