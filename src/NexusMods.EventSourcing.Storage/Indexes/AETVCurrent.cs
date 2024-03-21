using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.Indexes;

internal class AETVCurrent(AttributeRegistry registry) : AIndex(ColumnFamilyName, registry)
{
    private static string ColumnFamilyName => "AETVCurrent";

    [StructLayout(LayoutKind.Explicit, Size = sizeof(ushort) + sizeof(ulong))]
    private struct Key
    {
        [FieldOffset(0)]
        public ushort Attribute;

        [FieldOffset(sizeof(ushort))]
        public ulong Entity;
    }

    protected override int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var keyA = MemoryMarshal.Read<Key>(a);
        var keyB = MemoryMarshal.Read<Key>(b);

        var cmp = keyA.Attribute.CompareTo(keyB.Attribute);
        if (cmp != 0)
            return cmp;

        return keyA.Entity.CompareTo(keyB.Entity);
    }

    public void Add(WriteBatch batch, ref StackDatom stackDatom)
    {
        Key key = new()
        {
            Entity = stackDatom.E,
            Attribute = stackDatom.A
        };

        var valueSpan = stackDatom.Padded(sizeof(ulong));
        MemoryMarshal.Write(valueSpan, stackDatom.T);

        var keySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
        batch.Put(keySpan, valueSpan, ColumnFamily);
    }

    public IEnumerable<EntityId> GetEntitiesWithAttribute<TAttribute>(TxId asOf) where TAttribute : IAttribute
    {
        Key key = new()
        {
            Attribute = (ushort)Registry.GetAttributeId<TAttribute>(),
            Entity = 0
        };

        foreach (var value in Db.GetScopedIterator(key, ColumnFamily))
        {
            var keyRead = MemoryMarshal.Read<Key>(value.KeySpan);

            if (keyRead.Attribute != key.Attribute)
                break;

            var tx = MemoryMarshal.Read<TxId>(value.ValueSpan);
            if (tx <= asOf)
            {
                yield return EntityId.From(keyRead.Entity);
                continue;
            }

            throw new NotImplementedException();


        }

    }
}
