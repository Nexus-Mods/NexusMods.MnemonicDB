using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing;

/// <summary>
/// Abstract event store implementation, provides helpers for serialization and deserialization.
/// </summary>
public abstract class AEventStore : IEventStore
{
    private readonly IVariableSizeSerializer<string> _stringSerializer;
    private readonly IFixedSizeSerializer<EntityDefinition> _entityDefinitionSerializer;
    private readonly ISerializationRegistry _serializationRegistry;
    private readonly PooledMemoryBufferWriter _writer;

    /// <summary>
    /// Abstract event store implementation, provides helpers for serialization and deserialization.
    /// </summary>
    /// <param name="serializationRegistry"></param>
    public AEventStore(ISerializationRegistry serializationRegistry)
    {
        _stringSerializer = (serializationRegistry.GetSerializer(typeof(string)) as IVariableSizeSerializer<string>)!;
        _entityDefinitionSerializer = (serializationRegistry.GetSerializer(typeof(EntityDefinition)) as IFixedSizeSerializer<EntityDefinition>)!;
        _serializationRegistry = serializationRegistry;
        _writer = new PooledMemoryBufferWriter();

    }

    /// <summary>
    /// Deserializes a snapshot into a definition and a list of attributes.
    /// </summary>
    /// <param name="loadedDefinition"></param>
    /// <param name="loadedAttributes"></param>
    /// <param name="snapshot"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    protected bool DeserializeSnapshot(out IAccumulator loadedDefinition,
        out (IAttribute Attribute, IAccumulator Accumulator)[] loadedAttributes, ReadOnlySpan<byte> snapshot)
    {
        var entityDefinition = _entityDefinitionSerializer.Deserialize(snapshot.SliceFast(0, 18));

        var typeAccumulator = IEntity.TypeAttribute.CreateAccumulator();
        typeAccumulator.ReadFrom(ref snapshot, _serializationRegistry);


        var appDefinition = EntityStructureRegistry.GetDefinitionByUUID(entityDefinition.UUID);

        if (entityDefinition.Revision != appDefinition.Revision)
        {
            loadedAttributes = Array.Empty<(IAttribute, IAccumulator)>();
            loadedDefinition = default!;
            return false;
        }

        snapshot = snapshot.SliceFast(18);

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

            var valueRead = accumulator.ReadFrom(ref snapshot, _serializationRegistry);
            snapshot = snapshot.SliceFast(valueRead);

            results[i] = (attribute, accumulator);
        }

        loadedAttributes = results;
        loadedDefinition = typeAccumulator;
        return true;
    }

    /// <summary>
    /// Serializes a snapshot into a byte span, the span will be valid until the next call to a serialize method.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="attributes"></param>
    /// <returns></returns>
    protected ReadOnlySpan<byte> SerializeSnapshot(EntityId id, IDictionary<IAttribute, IAccumulator> attributes)
    {
        _writer.Reset();

        // Snapshot starts with the type attribute value
        var typeAccumulator = attributes[IEntity.TypeAttribute];
        typeAccumulator.WriteTo(_writer, _serializationRegistry);

        var sizeSpan = _writer.GetSpan(sizeof(ushort));
        BinaryPrimitives.WriteUInt16BigEndian(sizeSpan, (ushort) (attributes.Count - 1));
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
        return span;
    }


    /// <inheritdoc />
    public abstract TransactionId Add<TEntity, TColl>(TEntity eventEntity, TColl indexed)
        where TEntity : IEvent
        where TColl : IList<(IIndexableAttribute, IAccumulator)>;

    /// <inheritdoc />
    public abstract void EventsForIndex<TIngester, TVal>(IIndexableAttribute<TVal> attr, TVal value, TIngester ingester, TransactionId fromTx,
        TransactionId toTx) where TIngester : IEventIngester;

    /// <inheritdoc />
    public abstract TransactionId GetSnapshot(TransactionId asOf, EntityId entityId, out IAccumulator loadedDefinition,
        out (IAttribute Attribute, IAccumulator Accumulator)[] loadedAttributes);

    /// <inheritdoc />
    public abstract void SetSnapshot(TransactionId txId, EntityId id, IDictionary<IAttribute, IAccumulator> attributes);

    /// <inheritdoc />
    public abstract TransactionId TxId { get; }
}
