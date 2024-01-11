using System.Buffers;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.TestModel;

public class InMemoryEventStore<TSerializer> : IEventStore
where TSerializer : IEventSerializer
{
    private TransactionId _tx = TransactionId.From(0);
    private readonly Dictionary<EntityId,IList<(TransactionId TxId, byte[] Data)>> _events = new();
    private readonly Dictionary<EntityId, SortedDictionary<TransactionId, byte[]>> _snapshots = new();
    private TSerializer _serializer;
    private readonly IVariableSizeSerializer<string> _stringSerializer;
    private readonly PooledMemoryBufferWriter _writer;
    private readonly ISerializationRegistry _serializationRegistry;

    public InMemoryEventStore(TSerializer serializer, ISerializationRegistry serializationRegistry)
    {
        _serializer = serializer;
        _stringSerializer = (serializationRegistry.GetSerializer(typeof(string)) as IVariableSizeSerializer<string>)!;
        _serializationRegistry = serializationRegistry;
        _writer = new PooledMemoryBufferWriter();
    }

    public TransactionId Add<T>(T entity) where T : IEvent
    {
        lock (this)
        {
            _tx = _tx.Next();
            var data = _serializer.Serialize(entity);
            var logger = new ModifiedEntitiesIngester();
            entity.Apply(logger);
            foreach (var id in logger.Entities)
            {
                if (!_events.TryGetValue(id, out var value))
                {
                    value = new List<(TransactionId, byte[])>();
                    _events.Add(id, value);
                }

                value.Add((_tx, data.ToArray()));
            }

            return _tx;
        }
    }


    public void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester)
        where TIngester : IEventIngester
    {
        if (!_events.TryGetValue(entityId, out var events))
            return;

        foreach (var data in events)
        {
            var @event = _serializer.Deserialize(data.Data)!;
            if (!ingester.Ingest(data.TxId, @event)) break;
        }
    }

    public void EventsAndSnapshotForEntity<TIngester>(TransactionId asOf, EntityId entityId, TIngester ingester) where TIngester : ISnapshotEventIngester
    {
        if (!_snapshots.TryGetValue(entityId, out var snapshots))
            return;

        var startPoint = snapshots.LastOrDefault(s => s.Key <= asOf);

        if (startPoint != default)
        {
            var snapshot = startPoint.Value.AsSpanFast();

            while (snapshot.Length > 0)
            {
                var attributeName = _stringSerializer.Deserialize(snapshot, out var read);
                snapshot = snapshot.SliceFast(read);
                var accumulator = _serializationRegistry.GetAccumulator(attributeName);
                accumulator.ReadFrom(snapshot, _serializationRegistry, out read);
                snapshot = snapshot.Slice(read);
                ingester.IngestSnapshot(attributeName, accumulator);
            }

        }

        foreach (var (txId, data) in snapshots.)
        {
            var @event = _serializer.Deserialize(data)!;
            if (!ingester.Ingest(txId, @event)) break;
        }

        if (!_events.TryGetValue(entityId, out var events))
            return;

        foreach (var data in events)
        {
            var @event = _serializer.Deserialize(data.Data)!;
            if (!ingester.Ingest(data.TxId, @event)) break;
        }
    }

    public void SetSnapshot(TransactionId txId, EntityId id, IEnumerable<(string AttributeName, IAccumulator Accumulator)> attributes)
    {
        _writer.Reset();

        foreach (var (attributeName, accumulator) in attributes)
        {
            _stringSerializer.Serialize(attributeName, _writer);
            accumulator.WriteTo(_writer, _serializationRegistry);
        }

        var span = _writer.GetWrittenSpan();

        if (!_snapshots.TryGetValue(id, out var snapshots))
        {
            snapshots = new SortedDictionary<TransactionId, byte[]>();
            _snapshots.Add(id, snapshots);
        }

        snapshots.Add(txId, span.ToArray());
    }
}
