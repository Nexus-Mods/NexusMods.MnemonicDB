using System;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.Indexes;

public class EATVHistory
{
    private readonly ColumnFamilyHandle _columnFamily;
    private readonly RocksDb _db;
    private readonly AttributeRegistry _registry;
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

    public EATVHistory(RocksDb db, AttributeRegistry registry)
    {
        _registry = registry;
        _db = db;
        _columnFamily = db.GetColumnFamily(ColumnFamilyName);
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
        batch.Put(keySpan, datom.V, _columnFamily);
    }

    private class ValueDeserializer<TValueType> : ISpanDeserializer<TValueType>
    {
        private readonly IValueSerializer<TValueType> _serializer;

        public ValueDeserializer(AttributeRegistry registry)
        {
            _serializer = registry.GetSerializer<TValueType>();
        }

        public TValueType Deserialize(ReadOnlySpan<byte> span)
        {
            _serializer.Read(span, out var val);
            return val;
        }
    }

    public bool TryGetExact<TAttr, TValue>(EntityId entityId, TxId tx, out TValue val)
        where TAttr : IAttribute<TValue>
    {
        Key key = new()
        {
            Entity = entityId.Value,
            Tx = tx.Value,
            Attribute = (ushort)_registry.GetAttributeId<TAttr>()
        };
        using var tValue = _db.GetScoped(ref key, _columnFamily);
        if (!tValue.IsValid)
        {
            val = default!;
            return false;
        }
        val = _registry.Read<TAttr, TValue>(tValue.Span);
        return true;
    }

    public bool TryGetLatest<TAttribute, TValue>(EntityId entityId, TxId tx, out TValue foundVal)
        where TAttribute : IAttribute<TValue>
    {
        Key key = new()
        {
            Entity = entityId.Value,
            Tx = tx.Value,
            Attribute = (ushort)_registry.GetAttributeId<TAttribute>()
        };

        foreach (var value in _db.GetScopedIterator(key, _columnFamily))
        {
            var currKey = value.Key<Key>();
            if (currKey.Tx > tx.Value || currKey.Entity != entityId.Value || currKey.Attribute != key.Attribute)
                break;
            foundVal = _registry.Read<TAttribute, TValue>(value.ValueSpan);
            return true;
        }

        foundVal = default!;
        return false;
    }

}
