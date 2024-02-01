using System;
using System.Buffers.Binary;
using NexusMods.Paths;
using RocksDbSharp;

namespace NexusMods.EventSourcing.DatomBenchmarkTest;

public class DatomStore
{
    private readonly ColumnFamilies _families;
    private readonly RocksDb _db;
    private readonly ColumnFamilyHandle _eavt;

    public DatomStore(string path)
    {
        _families = new ColumnFamilies();
        _families.Add("eavt", new ColumnFamilyOptions());

        var options = new DbOptions();
        options.SetCreateIfMissing();
        options.SetCreateMissingColumnFamilies();
        options.SetCompression(Compression.Zstd);


        _db = RocksDb.Open(options,
            path.ToString(), _families);

        _eavt = _db.GetColumnFamily("eavt");

    }

    public void InsertDatom(ulong entityId, ulong attribute, ReadOnlySpan<byte> value, ulong tx)
    {
        Span<byte> entityIdSpan = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(entityIdSpan, entityId);
        Span<byte> attributeSpan = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(attributeSpan, attribute);
        Span<byte> txSpan = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(txSpan, tx);
        InsertDatom(entityIdSpan, attributeSpan, value, txSpan);
    }

    private void InsertDatom(ReadOnlySpan<byte> entityId, ReadOnlySpan<byte> attr, ReadOnlySpan<byte> value,
        ReadOnlySpan<byte> tx)
    {
        Span<byte> key = stackalloc byte[entityId.Length + attr.Length + value.Length + tx.Length];

        _db.Put(key, Span<byte>.Empty, _eavt);
    }
}
