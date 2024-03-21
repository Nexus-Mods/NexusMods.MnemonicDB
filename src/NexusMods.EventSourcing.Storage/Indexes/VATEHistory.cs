using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.Indexes;

public class VATEHistory(AttributeRegistry registry) : AIndex(ColumnFamilyName, registry)
{
    private static string ColumnFamilyName => "VATEHistory";

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 10)]
    private struct Key
    {
        public ushort AttributeId;
        public ulong TxId;
    }

    protected override int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var keyA = MemoryMarshal.Read<Key>(a);
        var keyB = MemoryMarshal.Read<Key>(b);

        // This looks like we compare A first, but really we're doing matching Vs first
        // and sorting Vs from different attributes by their value
        if (keyA.AttributeId != keyB.AttributeId)
            return keyA.AttributeId.CompareTo(keyB.AttributeId);

        var cmp = Registry.CompareValues(AttributeId.From(keyA.AttributeId), a.SliceFast(10), b.SliceFast(10));
        if (cmp != 0)
            return cmp;

        return -keyA.TxId.CompareTo(keyB.TxId);
    }


    public void Add(WriteBatch batch, ref StackDatom stackDatom)
    {
        var keySpan = stackDatom.Padded(10);
        var casted = MemoryMarshal.Cast<byte, Key>(keySpan);
        casted[0].AttributeId = stackDatom.A;
        casted[0].TxId = stackDatom.T;

        Span<byte> valueSpan = stackalloc byte[10];
        MemoryMarshal.Write(valueSpan, stackDatom.E);

        batch.Put(keySpan, valueSpan, ColumnFamily);
    }

    public IEnumerable<EntityId> GetReferencesToEntityThroughAttribute<TAttribute>(EntityId id, TxId tx)
        where TAttribute : IAttribute<EntityId>
    {
        var attributeId = Registry.GetAttributeId<TAttribute>();
        var key = new Key { AttributeId = (ushort)attributeId.Value, TxId = tx.Value };
        var keySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));

        using var iterator = Db.NewIterator(ColumnFamily);
        iterator.Seek(keySpan);
        var lastReadId = EntityId.From(ulong.MaxValue);
        while (iterator.Valid())
        {
            var currentKey = MemoryMarshal.Read<Key>(iterator.Key());
            if (currentKey.AttributeId != attributeId)
                break;

            if (currentKey.TxId < tx.Value)
                continue;

            var entityId = MemoryMarshal.Read<EntityId>(iterator.Value());
            if (entityId.Value != lastReadId)
                yield return entityId;
            lastReadId = entityId;
            iterator.Next();
        }
    }
}
