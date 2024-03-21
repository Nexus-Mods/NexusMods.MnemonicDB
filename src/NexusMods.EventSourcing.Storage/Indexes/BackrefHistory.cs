using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.Indexes;

/// <summary>
/// A index of all datoms where the value is a reference to an entity. Indexed as [value, attribute, tx] -> entity
/// where the value is always an entity ID.
/// </summary>
/// <param name="registry"></param>
public class BackrefHistory(AttributeRegistry registry, ColumnFamilies columnFamilies) : AIndex(ColumnFamilyName, registry, columnFamilies)
{
    private static string ColumnFamilyName => "VATEHistory";

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 10)]
    private struct Key
    {
        public ulong ReferenceId;
        public ushort AttributeId;
        public ulong TxId;
        public ulong EntityId;
    }

    protected override int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var keyA = MemoryMarshal.Read<Key>(a);
        var keyB = MemoryMarshal.Read<Key>(b);

        var cmp = keyA.ReferenceId.CompareTo(keyB.ReferenceId);
        if (cmp != 0)
            return cmp;

        cmp = keyA.AttributeId.CompareTo(keyB.AttributeId);
        if (cmp != 0)
            return cmp;

        cmp = -keyA.TxId.CompareTo(keyB.TxId);
        if (cmp != 0)
            return cmp;

        return keyA.EntityId.CompareTo(keyB.EntityId);
    }


    public void Add(WriteBatch batch, ref StackDatom stackDatom)
    {
        var key = new Key {
            ReferenceId = MemoryMarshal.Read<ulong>(stackDatom.V),
            AttributeId = stackDatom.A,
            TxId = stackDatom.T,
            EntityId = stackDatom.E
        };

        var keySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));

        batch.Put(keySpan, ReadOnlySpan<byte>.Empty, ColumnFamily);
    }

    public IEnumerable<EntityId> GetReferencesToEntityThroughAttribute<TAttribute>(EntityId id, TxId tx)
        where TAttribute : IAttribute<EntityId>
    {
        var attributeId = Registry.GetAttributeId<TAttribute>();
        var key = new Key
        {
            ReferenceId = id.Value,
            AttributeId = (ushort)attributeId.Value,
            TxId = tx.Value,
            EntityId = 0,
        };
        var keySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));

        using var iterator = Db.NewIterator(ColumnFamily);
        iterator.Seek(keySpan);
        var lastReadId = ulong.MaxValue;
        while (iterator.Valid())
        {
            var currentKey = MemoryMarshal.Read<Key>(iterator.Key());
            if (currentKey.ReferenceId != id.Value)
                yield break;

            if (currentKey.TxId > tx.Value)
                yield break;

            if (currentKey.AttributeId != attributeId.Value)
                continue;

            if (currentKey.EntityId != lastReadId)
                yield return EntityId.From(currentKey.EntityId);
            lastReadId = currentKey.EntityId;
            iterator.Next();
        }
    }
}
