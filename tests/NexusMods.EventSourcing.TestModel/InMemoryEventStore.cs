using System.Buffers;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.TestModel;

public class InMemoryEventStore<TSerializer> : AEventStore
where TSerializer : IEventSerializer
{
    private TransactionId _tx = TransactionId.From(0);
    private readonly Dictionary<EntityId,IList<(TransactionId TxId, byte[] Data)>> _events = new();
    private readonly Dictionary<EntityId, SortedDictionary<TransactionId, byte[]>> _snapshots = new();
    private TSerializer _serializer;

    public InMemoryEventStore(TSerializer serializer, ISerializationRegistry serializationRegistry) : base(serializationRegistry)
    {
        _serializer = serializer;
    }

    public override TransactionId Add<T>(T entity)
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


    public override void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester, TransactionId fromId, TransactionId toId)
    {
        if (!_events.TryGetValue(entityId, out var events))
            return;

        foreach (var data in events)
        {
            if (data.TxId < fromId) continue;
            if (data.TxId > toId) break;

            var @event = _serializer.Deserialize(data.Data)!;
            if (!ingester.Ingest(data.TxId, @event)) break;
        }
    }

    public override TransactionId GetSnapshot(TransactionId asOf, EntityId entityId, out IAccumulator loadedDefinition,
        out (IAttribute Attribute, IAccumulator Accumulator)[] loadedAttributes)
    {
        if (!_snapshots.TryGetValue(entityId, out var snapshots))
        {
            loadedAttributes = Array.Empty<(IAttribute, IAccumulator)>();
            loadedDefinition = default!;
            return TransactionId.Min;
        }

        var startPoint = snapshots.LastOrDefault(s => s.Key <= asOf);

        if (startPoint.Value == default)
        {
            loadedAttributes = Array.Empty<(IAttribute, IAccumulator)>();
            loadedDefinition = default!;
            return default;
        }

        var snapshot = (ReadOnlySpan<byte>)startPoint.Value.AsSpanFast();

        return DeserializeSnapshot(out loadedDefinition, out loadedAttributes, snapshot, startPoint);
    }

    public override void SetSnapshot(TransactionId txId, EntityId id, IDictionary<IAttribute, IAccumulator> attributes)
    {
        var span = SerializeSnapshot(id, attributes);

        if (!_snapshots.TryGetValue(id, out var snapshots))
        {
            snapshots = new SortedDictionary<TransactionId, byte[]>();
            _snapshots.Add(id, snapshots);
        }

        snapshots.Add(txId, span.ToArray());
    }
}
