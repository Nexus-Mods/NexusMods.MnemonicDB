using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.Indexes;

/// <summary>
/// A index that organizes data by (E, A, T, V) but only ever retains the current value and TX for each (E, A) pair, this
/// leverages the fact that users often only want the current value for a given attribute. Results can be filtered by
/// a given TX, and then the history index can be used to retrieve the full history of a given attribute to fill in the
/// filtered results.
/// </summary>
public class EATVCurrent(AttributeRegistry attributeRegistry, ColumnFamilies columnFamilies) : AIndex(ColumnFamilyName, attributeRegistry, columnFamilies)
{
    public static string ColumnFamilyName => "EATVCurrent";

    [StructLayout(LayoutKind.Explicit, Size = sizeof(ulong) + sizeof(ushort))]
    private unsafe struct Key
    {
        [FieldOffset(0)] public ulong Entity;
        [FieldOffset(sizeof(ulong))] public ushort Attribute;
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

    public LookupResult TryGet<TAttr, TValue>(EntityId e, TxId tx, out TValue val) where TAttr : IAttribute<TValue>
    {
        Key key = new()
        {
            Entity = e.Value,
            Attribute = (ushort)Registry.GetAttributeId<TAttr>()
        };

        using var tValue = Db.GetScoped(ref key, ColumnFamily);
        if (!tValue.IsValid)
        {
            val = default!;
            return LookupResult.NotFound;
        }

        var datomTx = MemoryMarshal.Read<TxId>(tValue.Span);
        if (datomTx <= tx)
        {
            val = Registry.Read<TAttr, TValue>(tValue.Span.SliceFast(sizeof(ulong)));
            return LookupResult.Found;
        }

        val = default!;
        return LookupResult.FoundNewer;
    }

    protected override int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var keyA = MemoryMarshal.Read<Key>(a);
        var keyB = MemoryMarshal.Read<Key>(b);

        var cmp = keyA.Entity.CompareTo(keyB.Entity);
        if (cmp != 0) return cmp;

        return keyA.Attribute.CompareTo(keyB.Attribute);
    }

    public IEnumerable<IReadDatom> GetAttributesForEntity(EntityId e, TxId txId)
    {
        var key = new Key
        {
            Entity = e.Value,
            Attribute = 0
        };

        foreach (var value in Db.GetScopedIterator(key, ColumnFamily))
        {
            var thisKey = value.Key<Key>();
            if (thisKey.Entity != e.Value)
                break;

            var thisTxId = MemoryMarshal.Read<TxId>(value.ValueSpan);
            var valueSpan = value.ValueSpan.SliceFast(sizeof(ulong));

            yield return Registry.Resolve(EntityId.From(thisKey.Entity),
                AttributeId.From(thisKey.Attribute), valueSpan, thisTxId);
        }
    }

    public EntityId GetMaxEntityId()
    {
        var seekKey = new Key
        {
            Entity = Ids.MakeId(Ids.Partition.Entity, ulong.MaxValue),
            Attribute = ushort.MaxValue
        };

        using var iterator = Db.NewIterator(ColumnFamily);
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref seekKey, 1));
        iterator.SeekForPrev(span);

        if (!iterator.Valid())
            return EntityId.From(Ids.MakeId(Ids.Partition.Entity, 0));

        var key = MemoryMarshal.Read<Key>(iterator.Key());

        return EntityId.From(key.Entity);
    }
}
