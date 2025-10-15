using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage.Abstractions;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage;

public partial class DatomStore
{
    // File format:
    // FOURCC: "MDBX"
    // ushort: version
    // one or more chunks
    // 
    // chunk:
    // byte: IndexType
    // uint: datomCount (number of datoms in the chunk)
    // uint: chunkSize (in bytes)
    // datomBlob
    
    private static readonly byte[] FourCC = "MDBX"u8.ToArray();
    private const int ChunkSize = 1024 * 16;
    
    /// <summary>
    /// Exports the database to the given stream
    /// </summary>
    public async Task ExportAsync(Stream stream)
    {
        await Task.Yield();
        var exportedDatoms = 0;
        var binaryWriter = new BinaryWriter(stream);
        binaryWriter.Write(FourCC);
        binaryWriter.Write((ushort)1);

        var snapshot = CurrentSnapshot;
        
        foreach (var indexType in Enum.GetValues<IndexType>())
        {
            if (indexType == IndexType.None)
                continue;
            
            var slice = SliceDescriptor.Create(indexType);
            var chunks = snapshot.DatomsChunked(slice, ChunkSize);

            throw new NotSupportedException();
            /*
            foreach (var chunk in chunks)
            {
                var data = chunk.Data;
                binaryWriter.Write((byte)indexType);
                binaryWriter.Write((uint)chunk.Count);
                binaryWriter.Write((uint)data.Length);
                binaryWriter.Write(data.Span);
                exportedDatoms += chunk.Count;
            }
            */
        }
        Logger.LogInformation("Exported {0} datoms", exportedDatoms);
    }

    /// <summary>
    /// Imports the database from the given stream, will delete all the data in the store before importing
    /// the new data
    /// </summary>
    public async Task ImportAsync(Stream stream)
    {
        await Task.Yield();
        CleanStore();
        var importedCount = 0;
        var binaryReader = new BinaryReader(stream);
        var fourCC = binaryReader.ReadBytes(4);
        if (!fourCC.SequenceEqual(FourCC))
            throw new InvalidDataException("Invalid file format");
        
        var version = binaryReader.ReadUInt16();
        if (version != 1)
            throw new InvalidDataException("Invalid file version");

        try
        {
            while (true)
            {
                var index = (IndexType)binaryReader.ReadByte();
                var datomCount = binaryReader.ReadUInt32();
                var chunkSize = binaryReader.ReadUInt32();
                var data = binaryReader.ReadBytes((int)chunkSize);
                var segment = new IndexSegment((int)datomCount, data.AsMemory(), AttributeCache);

                using var batch = Backend.CreateBatch();

                foreach (var datom in segment)
                    batch.Add(index, datom);

                batch.Commit();
                importedCount += (int)datomCount;
            }
        }
        catch (EndOfStreamException)
        {
            // End of stream
        }

        Logger.LogInformation("Imported {0} datoms", importedCount);
        _nextIdCache.ResetCaches();
        Bootstrap();
    }

    private void CleanStore()
    { 
        var datomCount = 0;
        var snapshot = Backend.GetSnapshot();
        using var batch = Backend.CreateBatch();
        foreach (var index in Enum.GetValues<IndexType>())
        {
            if (index == IndexType.None)
                continue;
            
            var slice = SliceDescriptor.Create(index);
            var datoms = snapshot.Datoms(slice);
            throw new NotImplementedException();
            /*
            foreach (var datom in datoms)
            {
                batch.Delete(index, datom);
                datomCount++;
            }*/
        }
        batch.Commit();
        Logger.LogInformation("Cleaned {0} datoms", datomCount);
    }
}
