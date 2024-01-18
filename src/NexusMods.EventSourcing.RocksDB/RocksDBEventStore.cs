using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;
using NexusMods.EventSourcing.Serialization;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.RocksDB;

public sealed class RocksDBEventStore<TSerializer> : AEventStore, IDisposable
    where TSerializer : IEventSerializer
{
    private readonly ColumnFamilies _families;
    private readonly RocksDb _db;
    private TransactionId _tx;
    private readonly ColumnFamilyHandle _eventsColumn;
    private readonly TSerializer _serializer;
    private readonly SpanDeserializer<TSerializer> _deserializer;
    private readonly ColumnFamilyHandle _snapshotColumn;
    private readonly Dictionary<IIndexableAttribute,ColumnFamilyHandle> _indexColumns;

    public RocksDBEventStore(TSerializer serializer, Settings settings, ISerializationRegistry serializationRegistry) : base(serializationRegistry)
    {
        _serializer = serializer;
        _families = new ColumnFamilies();
        _families.Add("events", new ColumnFamilyOptions());
        _families.Add("entityIndex", new ColumnFamilyOptions());
        var options = new DbOptions();
        options.SetCreateIfMissing();
        _db = RocksDb.Open(options,
            settings.StorageLocation.ToString(), new ColumnFamilies());

        var indexableAttributes = EntityStructureRegistry.AllIndexableAttributes();

        _indexColumns = new Dictionary<IIndexableAttribute, ColumnFamilyHandle>();
        foreach (var attr in indexableAttributes.Distinct())
        {
            _indexColumns.Add(attr,
                _db.CreateColumnFamily(new ColumnFamilyOptions(), "index_" + attr.IndexedAttributeId.ToString("X")));
        }

        _eventsColumn = _db.CreateColumnFamily(new ColumnFamilyOptions(), "events");
        _snapshotColumn = _db.CreateColumnFamily(new ColumnFamilyOptions(), "snapshots");
        _tx = TransactionId.From(0);

        _deserializer = new SpanDeserializer<TSerializer>(serializer);
    }

    /// <inheritdoc />
    public override TransactionId Add<TEntity, TColl>(TEntity eventEntity, TColl indexed)
    {
        _tx = _tx.Next();

        var eventSpan = _serializer.Serialize(eventEntity);

        Span<byte> txIdSpan = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(txIdSpan, _tx.Value);

        _db.Put(txIdSpan, eventSpan, _eventsColumn);

        var size = indexed.Count;
        for (var index = 0; index < size; index++)
        {
            var (attr, accumulator) = indexed[index];
            PutIndex(attr, accumulator, _tx);
        }

        return _tx;
    }

    private void PutIndex(IIndexableAttribute attr, IAccumulator accumulator, TransactionId txId)
    {
        var valueSize = attr.SpanSize();

        Span<byte> keySpan = stackalloc byte[valueSize + 8];
        attr.WriteTo(keySpan.SliceFast(0, valueSize), accumulator);
        BinaryPrimitives.WriteUInt64BigEndian(keySpan.SliceFast(valueSize), txId.Value);

        _db.Put(keySpan, ReadOnlySpan<byte>.Empty, _indexColumns[attr]);
    }

    public override void EventsForIndex<TIngester, TVal>(IIndexableAttribute<TVal> attr, TVal value, TIngester ingester, TransactionId fromTx,
        TransactionId toTx)
    {
        var valueSize = attr.SpanSize();

        Span<byte> startKey = stackalloc byte[valueSize + 8];
        Span<byte> endKey = stackalloc byte[valueSize + 8];

        attr.WriteTo(startKey.SliceFast(0, valueSize), value);
        BinaryPrimitives.WriteUInt64BigEndian(startKey.SliceFast(valueSize), fromTx.Value);

        attr.WriteTo(endKey.SliceFast(0, valueSize), value);
        BinaryPrimitives.WriteUInt64BigEndian(endKey.SliceFast(valueSize), toTx.Value == TransactionId.Max ? toTx.Value : toTx.Value + 1);

        var options = new ReadOptions();
        unsafe
        {
            fixed (byte* startKeyPtr = startKey)
            {
                fixed (byte* endKeyPtr = endKey)
                {
                    options.SetIterateUpperBound(endKeyPtr, (ulong)(valueSize + 8));
                    options.SetIterateLowerBound(startKeyPtr, (ulong)(valueSize + 8));

                    using var iterator = _db.NewIterator(_indexColumns[attr], options);

                    iterator.SeekToFirst();

                    while (iterator.Valid())
                    {

                        var keySpan = iterator.GetKeySpan();

                        var txSpan = keySpan.SliceFast(valueSize);
                        var txId = BinaryPrimitives.ReadUInt64BigEndian(txSpan);

                        var @event = _db.Get(txSpan, _deserializer, _eventsColumn);
                        if (!ingester.Ingest(TransactionId.From(txId), @event))
                            break;

                        iterator.Next();
                    }
                }
            }
        }


    }

    public override TransactionId GetSnapshot(TransactionId asOf, EntityId entityId, out IAccumulator loadedDefinition,
        out (IAttribute Attribute, IAccumulator Accumulator)[] loadedAttributes)
    {
        Span<byte> startKey = stackalloc byte[24];
        entityId.TryWriteBytes(startKey);
        BinaryPrimitives.WriteUInt64BigEndian(startKey.SliceFast(16), 0);
        Span<byte> endKey = stackalloc byte[24];
        entityId.TryWriteBytes(endKey);
        BinaryPrimitives.WriteUInt64BigEndian(endKey.SliceFast(16), asOf.Value);

        var options = new ReadOptions();
        unsafe
        {
            fixed (byte* startKeyPtr = startKey)
            {
                fixed (byte* endKeyPtr = endKey)
                {
                    //options.SetIterateUpperBound(endKeyPtr, 24);
                    //options.SetIterateLowerBound(startKeyPtr, 24);
                    using var iterator = _db.NewIterator(_snapshotColumn, options);

                    // Iterators are top end exclusive, so we need to seek to the last item before the asOf
                    iterator.SeekForPrev(endKeyPtr, 24);
                    while (iterator.Valid())
                    {
                        var key = iterator.GetKeySpan();
                        var txId = TransactionId.From(key.SliceFast(16));
                        var snapshotData = iterator.GetValueSpan();

                        if (snapshotData.Length == 0)
                        {
                            loadedAttributes = Array.Empty<(IAttribute, IAccumulator)>();
                            loadedDefinition = default!;
                            return TransactionId.Min;
                        }

                        if (!DeserializeSnapshot(out var foundDefinition, out var foundAttributes, snapshotData))
                        {
                            loadedAttributes = Array.Empty<(IAttribute, IAccumulator)>();
                            loadedDefinition = default!;
                            return TransactionId.Min;
                        }

                        loadedAttributes = foundAttributes;
                        loadedDefinition = foundDefinition;

                        return txId;
                    }
                }
            }
        }

        loadedAttributes = Array.Empty<(IAttribute, IAccumulator)>();
        loadedDefinition = default!;
        return TransactionId.Min;
    }

    public override void SetSnapshot(TransactionId txId, EntityId id, IDictionary<IAttribute, IAccumulator> attributes)
    {
        var span = SerializeSnapshot(id, attributes);

        Span<byte> keySpan = stackalloc byte[24];
        id.TryWriteBytes(keySpan);
        BinaryPrimitives.WriteUInt64BigEndian(keySpan.SliceFast(16), txId.Value);
        _db.Put(keySpan, span, _snapshotColumn);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _db.Dispose();
    }
}
