using System.Buffers;
using System.Buffers.Binary;
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
    private readonly IVariableSizeSerializer<EntityDefinition> _entityDefinitionSerializer;

    public InMemoryEventStore(TSerializer serializer, ISerializationRegistry serializationRegistry)
    {
        _serializer = serializer;
        _stringSerializer = (serializationRegistry.GetSerializer(typeof(string)) as IVariableSizeSerializer<string>)!;
        _entityDefinitionSerializer = (serializationRegistry.GetSerializer(typeof(EntityDefinition)) as IVariableSizeSerializer<EntityDefinition>)!;
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


    public void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester, TransactionId fromId, TransactionId toId)
        where TIngester : IEventIngester
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

    public TransactionId GetSnapshot(TransactionId asOf, EntityId entityId, ushort revision,
        out (IAttribute Attribute, IAccumulator Accumulator)[] loadedAttributes)
    {
        if (!_snapshots.TryGetValue(entityId, out var snapshots))
        {
            loadedAttributes = Array.Empty<(IAttribute, IAccumulator)>();
            return default;
        }

        var startPoint = snapshots.LastOrDefault(s => s.Key <= asOf);

        if (startPoint.Value == default)
        {
            loadedAttributes = Array.Empty<(IAttribute, IAccumulator)>();
            return default;
        }

        var snapshot = (ReadOnlySpan<byte>)startPoint.Value.AsSpanFast();
        var offset = _entityDefinitionSerializer.Deserialize(snapshot, out var entityDefinition);

        if (entityDefinition.Revision != revision)
        {
            loadedAttributes = Array.Empty<(IAttribute, IAccumulator)>();
            return default;
        }

        snapshot = snapshot.SliceFast(offset);

        var numberOfAttrs = BinaryPrimitives.ReadUInt16BigEndian(snapshot);
        snapshot = snapshot.SliceFast(sizeof(ushort));

        var results = GC.AllocateUninitializedArray<(IAttribute, IAccumulator)>(numberOfAttrs);

        if (!EntityStructureRegistry.TryGetAttributes(entityDefinition.Type, out var attributes))
            throw new Exception("Entity definition does not match the current structure registry.");

        for (var i = 0; i < numberOfAttrs; i++)
        {
            var read = _stringSerializer.Deserialize(snapshot, out var attributeName);
            snapshot = snapshot.SliceFast(read);

            if (!attributes.TryGetValue(attributeName, out var attribute))
                throw new Exception("Entity definition does not match the current structure registry.");

            var accumulator = attribute.CreateAccumulator();

            accumulator.ReadFrom(ref snapshot, _serializationRegistry);
            snapshot = snapshot.SliceFast(read);

            results[i] = (attribute, accumulator);
        }

        loadedAttributes = results;
        return startPoint.Key;
    }

    public void SetSnapshot(TransactionId txId, EntityId id, IDictionary<IAttribute, IAccumulator> attributes)
    {
        _writer.Reset();

        // Snapshot starts with the type attribute value
        var typeAccumulator = attributes[IEntity.TypeAttribute];
        typeAccumulator.WriteTo(_writer, _serializationRegistry);

        var sizeSpan = _writer.GetSpan(sizeof(ushort));
        BinaryPrimitives.WriteUInt16BigEndian(sizeSpan, (ushort) attributes.Count);
        _writer.Advance(sizeof(ushort));


        // And then each attribute in any order
        foreach (var (attribute, accumulator) in attributes)
        {
            if (attribute == IEntity.TypeAttribute) continue;

            var attributeName = attribute.Name;
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
