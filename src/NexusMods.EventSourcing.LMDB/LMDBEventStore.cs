using System;
using System.Buffers.Binary;
using LightningDB;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.LMDB;

public class LMDBEventStore<TSerializer> : IEventStore
    where TSerializer : IEventSerializer
{
    private readonly TSerializer _serializer;
    private readonly Settings _settings;
    private readonly LightningEnvironment _env;
    private TransactionId _tx;

    public LMDBEventStore(TSerializer serializer, Settings settings)
    {
        _serializer = serializer;
        _settings = settings;

        _env = new LightningEnvironment(_settings.StorageLocation.ToString());
        _env.MapSize = 1024L * 1024L; // 1 TiB
        _env.Open();
        _tx = TransactionId.From(0);
    }

    public TransactionId Add<T>(T eventValue) where T : IEvent
    {
        using var tx = _env.BeginTransaction();

        lock (this)
        {
            _tx = _tx.Next();

            {
                using var db = tx.OpenDatabase("events");
                Span<byte> keySpan = stackalloc byte[8];
                BinaryPrimitives.WriteUInt64BigEndian(keySpan, _tx.Value);
                var serialized = _serializer.Serialize(eventValue);
                tx.Put(db, keySpan, serialized);
            }

            {
                using var db = tx.OpenDatabase("entityIndex");
                var ingester = new ModifiedEntitiesIngester();
                eventValue.Apply(ingester);
                Span<byte> keySpan = stackalloc byte[24];
                BinaryPrimitives.WriteUInt64BigEndian(keySpan[16..], _tx.Value);
                foreach (var entityId in ingester.Entities)
                {
                    entityId.Value.TryWriteBytes(keySpan);
                    tx.Put(db, keySpan, keySpan);
                }
            }
            tx.Commit();
            return _tx;
        }
    }

    public void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester) where TIngester : IEventIngester
    {
        using var tx = _env.BeginTransaction(TransactionBeginFlags.ReadOnly);
        using var dbEIdx = tx.OpenDatabase("entityIndex");
        using var dbEvents = tx.OpenDatabase("events");

        using var cursor = tx.CreateCursor(dbEIdx);

        Span<byte> startKey = stackalloc byte[24];
        entityId.Value.TryWriteBytes(startKey);
        Span<byte> endKey = stackalloc byte[24];
        entityId.Value.TryWriteBytes(endKey);
        BinaryPrimitives.WriteUInt64BigEndian(endKey[16..], ulong.MaxValue);

        cursor.SetRange(startKey);

        while (true)
        {
            var (result, key, _) = cursor.GetCurrent();
            if (result != MDBResultCode.Success)
            {
                break;
            }

            var id = EntityId.From(new Guid(key.AsSpan()[..16]));
            if (id != entityId)
            {
                break;
            }

            var eventData = tx.Get(dbEvents, key.AsSpan()[16..]);
            if (eventData.resultCode != MDBResultCode.Success)
            {
                break;
            }
            var evt = _serializer.Deserialize(eventData.value.AsSpan());
            ingester.Ingest(evt);
        }
    }
}
