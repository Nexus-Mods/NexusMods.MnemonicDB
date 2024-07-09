using System;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Abstractions.Backup;

public class Exporter
{
    /// <summary>
    /// Write the TxLog to the given path.
    /// </summary>
    /// <param name="db"></param>
    /// <param name="path"></param>
    public async Task ExportAsync(IDb db, AbsolutePath path)
    {
        var from = new Datom(KeyPrefix.Max, ReadOnlyMemory<byte>.Empty, db.Registry);
        var to = new Datom(KeyPrefix.Min, ReadOnlyMemory<byte>.Empty, db.Registry);
        var descriptor = SliceDescriptor.Create(IndexType.TxLog, from, to);
        var maxTx = db.Snapshot.DatomsChunked(descriptor, 1).First().First().T;
        
        await using var file = path.Create();
        await using var compress = new GZipStream(file, CompressionLevel.Optimal);
        
        var sizeBuffer = new byte[sizeof(uint)];
        for (var tx = TxId.MinValue; tx <= maxTx; tx = TxId.From(tx.Value + 1))
        {
            var datoms = db.Datoms(tx);
            if (datoms.Count == 0)
                continue;
            MemoryMarshal.Write(sizeBuffer, datoms.DataSize);
            compress.Write(sizeBuffer);
            compress.Write(datoms.DataSpan);
        }
    }
}
