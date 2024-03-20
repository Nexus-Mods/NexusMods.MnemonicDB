using System;
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
public class EATVCurrent
{
    private readonly PooledMemoryBufferWriter _writer;
    private readonly ColumnFamilyHandle _columnFamily;
    private readonly RocksDb _db;
    private static readonly SpanDeserializer Deserializer = new();
    private readonly AttributeRegistry _registry;

    public static string ColumnFamilyName => "EATVCurrent";

    public EATVCurrent(RocksDb db, AttributeRegistry attributeRegistry)
    {
        _registry = attributeRegistry;
        _db = db;
        _columnFamily = db.GetColumnFamily("EATVCurrent");
        _writer = new PooledMemoryBufferWriter();
    }

    [StructLayout(LayoutKind.Explicit, Size = sizeof(ulong) + sizeof(ushort))]
    private unsafe struct Key
    {
        [FieldOffset(0)] public ulong Entity;
        [FieldOffset(sizeof(ulong))] public ushort Attribute;
    }

    private class SpanDeserializer : ISpanDeserializer<(TxId, byte[])>
    {
        public (TxId, byte[]) Deserialize(ReadOnlySpan<byte> buffer)
        {
            var tx = MemoryMarshal.Read<TxId>(buffer);
            return (tx, buffer.SliceFast(sizeof(ulong)).ToArray());
        }
    }


    public Datom Get(EntityId id, AttributeId attributeId)
    {
        Span<byte> key = stackalloc byte[sizeof(ulong) * sizeof(ushort)];
        MemoryMarshal.Write(key, id.Value);
        MemoryMarshal.Write(key.SliceFast(sizeof(ulong)), (ushort)attributeId.Value);

        // Still some wasted memory usage here, but oh well
        var (tx, valData) = _db.Get(key, Deserializer, _columnFamily);

        return new Datom
        {
            E = id,
            A = attributeId,
            T = tx,
            V = valData
        };
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
        batch.Put(keySpan, valueSpan, _columnFamily);
    }

    public LookupResult TryGet<TAttr, TValue>(EntityId e, TxId tx, out TValue val) where TAttr : IAttribute<TValue>
    {
        Key key = new()
        {
            Entity = e.Value,
            Attribute = (ushort)_registry.GetAttributeId<TAttr>()
        };

        using var tValue = _db.GetScoped(ref key, _columnFamily);
        if (!tValue.IsValid)
        {
            val = default!;
            return LookupResult.NotFound;
        }

        var datomTx = MemoryMarshal.Read<TxId>(tValue.Span);
        if (datomTx <= tx)
        {
            val = _registry.Read<TAttr, TValue>(tValue.Span.SliceFast(sizeof(ulong)));
            return LookupResult.Found;
        }

        val = default!;
        return LookupResult.FoundNewer;
    }
}
