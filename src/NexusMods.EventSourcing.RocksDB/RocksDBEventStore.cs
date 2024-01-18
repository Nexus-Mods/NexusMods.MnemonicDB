using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;
using NexusMods.EventSourcing.Serialization;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.RocksDB;

public sealed class RocksDBEventStore<TSerializer> : AEventStore
    where TSerializer : IEventSerializer
{
    private readonly ColumnFamilies _families;
    private readonly RocksDb _db;
    private TransactionId _tx;
    private readonly ColumnFamilyHandle _eventsColumn;
    private readonly ColumnFamilyHandle _entityIndexColumn;
    private readonly TSerializer _serializer;
    private readonly SpanDeserializer<TSerializer> _deserializer;
    private readonly ColumnFamilyHandle _snapshotColumn;

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
        _eventsColumn = _db.CreateColumnFamily(new ColumnFamilyOptions(), "events");
        _entityIndexColumn = _db.CreateColumnFamily(new ColumnFamilyOptions(), "entityIndex");
        _snapshotColumn = _db.CreateColumnFamily(new ColumnFamilyOptions(), "snapshots");
        _tx = TransactionId.From(0);

        _deserializer = new SpanDeserializer<TSerializer>(serializer);
    }

    public override TransactionId Add<T>(T eventEntity, (IIndexableAttribute, IAccumulator)[] indexed)
    {
        throw new NotImplementedException();
    }

    public override void EventsForIndex<TIngester, TVal>(IIndexableAttribute<TVal> attr, TVal value, TIngester ingester, TransactionId fromTx,
        TransactionId toTx)
    {
        throw new NotImplementedException();
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
}
