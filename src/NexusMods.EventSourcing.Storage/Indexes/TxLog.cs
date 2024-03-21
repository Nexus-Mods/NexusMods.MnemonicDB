using System;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.Indexes;

public class TxLog(AttributeRegistry registry) : AIndex(ColumnFamilyName, registry)
{
    private static string ColumnFamilyName => "TxLog";

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct Key
    {
        public readonly ulong Tx;
        public readonly ulong Entity;
        public readonly ushort Attribute;

        public Key(ulong tx, ulong entity, ushort attribute)
        {
            Tx = tx;
            Entity = entity;
            Attribute = attribute;
        }
    }

    protected override int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var keyA = MemoryMarshal.Read<Key>(a);
        var keyB = MemoryMarshal.Read<Key>(b);

        var cmp = keyA.Tx.CompareTo(keyB.Tx);
        if (cmp != 0) return cmp;

        cmp = keyA.Entity.CompareTo(keyB.Entity);
        if (cmp != 0) return cmp;

        return keyA.Attribute.CompareTo(keyB.Attribute);
    }

    public void Add(WriteBatch batch, ref StackDatom datom)
    {
        var key = new Key(datom.T, datom.E, datom.A);

        var keySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
        batch.Put(keySpan, datom.V, ColumnFamily);
    }

    public TxId GetMostRecentTxId()
    {
        using var iterator = Db.NewIterator(ColumnFamily);
        iterator.SeekToLast();
        if (!iterator.Valid())
            return TxId.MinValue;

        var key = MemoryMarshal.Read<Key>(iterator.Key());
        return TxId.From(key.Tx);

    }
}
