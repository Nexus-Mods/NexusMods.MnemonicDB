using System;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.Indexes;

public class EATVHistory(AttributeRegistry registry, ColumnFamilies columnFamilies) : AIndex(ColumnFamilyName, registry, columnFamilies)
{
    public static string ColumnFamilyName => "EATVHistory";

    [StructLayout(LayoutKind.Explicit, Size = sizeof(ulong) + sizeof(ulong) + sizeof(ushort))]
    private unsafe struct Key
    {
        [FieldOffset(0)]
        public ulong Entity;
        [FieldOffset(sizeof(ulong))]
        public ulong Tx;
        [FieldOffset(sizeof(ulong) + sizeof(ulong))]
        public ushort Attribute;
    }


    public void Add(WriteBatch batch, ref StackDatom datom)
    {
        Key key = new()
        {
            Entity = datom.E,
            Tx = datom.T,
            Attribute = datom.A
        };
        var keySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
        batch.Put(keySpan, datom.V, ColumnFamily);
    }


    public bool TryGetExact<TAttr, TValue>(EntityId entityId, TxId tx, out TValue val)
        where TAttr : IAttribute<TValue>
    {
        Key key = new()
        {
            Entity = entityId.Value,
            Tx = tx.Value,
            Attribute = (ushort)Registry.GetAttributeId<TAttr>()
        };
        using var tValue = Db.GetScoped(ref key, ColumnFamily);
        if (!tValue.IsValid)
        {
            val = default!;
            return false;
        }
        val = Registry.Read<TAttr, TValue>(tValue.Span);
        return true;
    }

    public bool TryGetLatest<TAttribute, TValue>(EntityId entityId, TxId tx, out TValue foundVal)
        where TAttribute : IAttribute<TValue>
    {
        Key key = new()
        {
            Entity = entityId.Value,
            Tx = tx.Value,
            Attribute = (ushort)Registry.GetAttributeId<TAttribute>()
        };

        foreach (var value in Db.GetScopedIterator(key, ColumnFamily))
        {
            var currKey = value.Key<Key>();
            if (currKey.Tx > tx.Value || currKey.Entity != entityId.Value || currKey.Attribute != key.Attribute)
                break;
            foundVal = Registry.Read<TAttribute, TValue>(value.ValueSpan);
            return true;
        }

        foundVal = default!;
        return false;
    }

    protected override int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var casted = MemoryMarshal.Read<Key>(a);
        var other = MemoryMarshal.Read<Key>(b);

        var cmp = casted.Entity.CompareTo(other.Entity);
        if (cmp != 0)
            return cmp;

        cmp = casted.Attribute.CompareTo(other.Attribute);
        if (cmp != 0)
            return cmp;

        // Reverse order so that we can get the latest value by iterating forward which may be slightly
        // faster, but also simpler to implement.
        return -casted.Tx.CompareTo(other.Tx);
    }
}
