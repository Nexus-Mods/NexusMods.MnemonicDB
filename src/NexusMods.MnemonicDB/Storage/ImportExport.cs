using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;

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
        var exportedDatoms = 0;
        var binaryWriter = new BinaryWriter(stream);
        binaryWriter.Write(FourCC);
        binaryWriter.Write((ushort)1);

        var snapshot = _backend.GetSnapshot();
        
        foreach (var indexType in Enum.GetValues<IndexType>())
        {
            var slice = SliceDescriptor.Create(indexType);
            var chunks = snapshot.DatomsChunked(slice, ChunkSize);

            foreach (var chunk in chunks)
            {
                var data = chunk.Data;
                binaryWriter.Write((byte)indexType);
                binaryWriter.Write((uint)chunk.Count);
                binaryWriter.Write((uint)data.Length);
                binaryWriter.Write(data.Span);
                exportedDatoms += chunk.Count;
            }
        }
        _logger.LogInformation("Exported {0} datoms", exportedDatoms);
    }

    public async Task ImportAsync(Stream stream)
    {
        CleanStore();
        var importedCount = 0;
        var binaryReader = new BinaryReader(stream);
        var fourCC = binaryReader.ReadBytes(4);
        if (!fourCC.SequenceEqual(FourCC))
            throw new InvalidDataException("Invalid file format");
        
        var version = binaryReader.ReadUInt16();
        if (version != 1)
            throw new InvalidDataException("Invalid file version");

        while (stream.Position < stream.Length)
        {
            var indexType = (IndexType)binaryReader.ReadByte();
            var datomCount = binaryReader.ReadUInt32();
            var chunkSize = binaryReader.ReadUInt32();
            var data = binaryReader.ReadBytes((int)chunkSize);
            var segment = new IndexSegment((int)datomCount, data.AsMemory(), _backend.AttributeCache);
            
            using var batch = _backend.CreateBatch();
            var index = _backend.GetIndex(indexType);
            
            foreach (var datom in segment) 
                index.Put(batch, datom);
            
            batch.Commit();
            importedCount += (int)datomCount;
        }
        
        _logger.LogInformation("Imported {0} datoms", importedCount);
        _nextIdCache.ResetCaches();
        Bootstrap();
    }

    private void CleanStore()
    { 
        int datomCount = 0;
        var snapshot = _backend.GetSnapshot();
        using var batch = _backend.CreateBatch();
        foreach (var index in Enum.GetValues<IndexType>())
        {
            var slice = SliceDescriptor.Create(index);
            var datoms = snapshot.Datoms(slice);
            foreach (var datom in datoms)
            {
                _backend.GetIndex(index).Delete(batch, datom);
                datomCount++;
            }
        }
        batch.Commit();
        _logger.LogInformation("Cleaned {0} datoms", datomCount);
    }
}