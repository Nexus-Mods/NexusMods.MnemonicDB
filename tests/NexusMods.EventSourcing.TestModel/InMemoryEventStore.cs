using System.Buffers;
using System.Buffers.Binary;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;
using NexusMods.Hashing.xxHash64;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.TestModel;

public class InMemoryEventStore<TSerializer> : AEventStore
where TSerializer : IEventSerializer
{
    private readonly List<byte[]> _events = new();
    private readonly Dictionary<(IAttribute, Hash), SortedSet<TransactionId>> _indexes = new();
    private readonly Dictionary<EntityId, SortedDictionary<TransactionId, byte[]>> _snapshots = new();
    private TSerializer _serializer;

    public InMemoryEventStore(TSerializer serializer, ISerializationRegistry serializationRegistry) : base(serializationRegistry)
    {
        _serializer = serializer;
        // Make the first item 0 so we don't ever issue a TX Id of 0
        _events.Add(Array.Empty<byte>());
    }

    public override TransactionId Add<T>(T entity, (IIndexableAttribute, IAccumulator)[] indexed)
    {
        lock (this)
        {
            // Create the new txId
            var txId = TransactionId.From((ulong)_events.Count);

            var data = _serializer.Serialize(entity);
            _events.Add(data.ToArray());

            foreach (var (attr, accumulator) in indexed)
            {
                // Hash the accumulator to condense it down into a single ulong
                var hash = HashAccumulator(attr, accumulator);

                if (_indexes.TryGetValue((attr, hash), out var found))
                {
                    found.Add(txId);
                }
                else
                {
                    var newSet = new SortedSet<TransactionId> { txId };
                    _indexes.Add((attr, hash), newSet);
                }
            }
            return txId;
        }
    }

    private static Hash HashAccumulator(IIndexableAttribute attr, IAccumulator accumulator)
    {
        Span<byte> span = stackalloc byte[attr.SpanSize()];
        attr.WriteTo(span, accumulator);
        var hash = span.XxHash64();
        return hash;
    }

    public override void EventsForIndex<TIngester, TVal>(IIndexableAttribute<TVal> attr, TVal value, TIngester ingester, TransactionId fromTx,
        TransactionId toTx)
    {
        Span<byte> valueSpan = stackalloc byte[attr.SpanSize()];
        attr.WriteTo(valueSpan, value);
        var hash = valueSpan.XxHash64();

        if (!_indexes.TryGetValue((attr, hash), out var found))
            return;

        foreach (var txId in found)
        {
            if (txId > toTx || txId < fromTx) continue;

            var eventItem = _serializer.Deserialize(_events[(int)txId.Value]);
            ingester.Ingest(txId, eventItem);
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

        if (DeserializeSnapshot(out loadedDefinition, out loadedAttributes, snapshot))
            return startPoint.Key;

        loadedAttributes = Array.Empty<(IAttribute, IAccumulator)>();
        loadedDefinition = default!;
        return default;
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
