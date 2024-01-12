using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
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

    public RocksDBEventStore(TSerializer serializer, Settings settings, SerializationRegistry serializationRegistry) : base(serializationRegistry)
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


    public override TransactionId Add<T>(T eventValue)
    {
        lock (this)
        {
             _tx = _tx.Next();

             // Write the event itself
             {
                 Span<byte> keySpan = stackalloc byte[8];
                 _tx.WriteTo(keySpan);
                 var span = _serializer.Serialize(eventValue);
                 _db.Put(keySpan, span, _eventsColumn);
             }

             // Update the entity indexes to mark them as having this event
             {
                 var ingester = new ModifiedEntitiesIngester();
                 eventValue.Apply(ingester);
                 Span<byte> keySpan = stackalloc byte[24];
                 _tx.WriteTo(keySpan.SliceFast(16..));
                 foreach (var entityId in ingester.Entities)
                 {
                     entityId.TryWriteBytes(keySpan);
                     _db.Put(keySpan, keySpan, _entityIndexColumn);
                 }
             }
             return _tx;
        }
    }

    public override void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester, TransactionId fromId, TransactionId toId)
    {
        Span<byte> startKey = stackalloc byte[24];
        entityId.TryWriteBytes(startKey);
        BinaryPrimitives.WriteUInt64BigEndian(startKey.SliceFast(16), fromId.Value);
        Span<byte> endKey = stackalloc byte[24];
        entityId.TryWriteBytes(endKey);
        BinaryPrimitives.WriteUInt64BigEndian(endKey.SliceFast(16), toId.Value);

        var options = new ReadOptions();
        unsafe
        {
            fixed (byte* startKeyPtr = startKey)
            {
                fixed (byte* endKeyPtr = endKey)
                {
                    options.SetIterateUpperBound(endKeyPtr, 24);
                    options.SetIterateLowerBound(startKeyPtr, 24);
                    using var iterator = _db.NewIterator(_entityIndexColumn, options);

                    iterator.SeekToFirst();
                    while (iterator.Valid())
                    {
                        var key = iterator.GetKeySpan();
                        var txId = TransactionId.From(key);
                        var evt = _db.Get(key[16..], _deserializer, _eventsColumn);
                        if (!ingester.Ingest(txId, evt)) break;
                        iterator.Next();
                    }
                }
            }
        }
    }

    public TransactionId GetSnapshot(TransactionId asOf, EntityId entityId, out IAccumulator loadedDefinition,
        out (IAttribute Attribute, IAccumulator Accumulator)[] loadedAttributes)
    {
        throw new NotImplementedException();
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
