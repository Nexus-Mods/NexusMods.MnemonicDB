using System;
using System.Buffers;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Abstractions;

public class EntityIdDefinition : IAttribute<EntityDefinitionAccumulator>, IIndexableAttribute<EntityId>
{
    /// <inheritdoc />
    public Type Owner => typeof(IEntity);

    /// <inheritdoc />
    public string Name => "Id";
    public EntityDefinitionAccumulator CreateAccumulator()
    {
        return new EntityDefinitionAccumulator();
    }

    IAccumulator IAttribute.CreateAccumulator()
    {
        return new EntityDefinitionAccumulator();
    }

    public void WriteTo(Span<byte> span, EntityId value)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public UInt128 IndexedAttributeId { get; set; }

    /// <inheritdoc />
    public int SpanSize()
    {
        return 16;
    }

    /// <inheritdoc />
    public void WriteTo(Span<byte> span, IAccumulator accumulator)
    {

    }
}

internal class EntityDefinitionAccumulator : IAccumulator
{
    internal EntityId Id;

    public void WriteTo(IBufferWriter<byte> writer, ISerializationRegistry registry)
    {
        var span = writer.GetSpan(16);
        BinaryPrimitives.WriteUInt128BigEndian(span, Id.Value);
        writer.Advance(16);
    }

    public int ReadFrom(ref ReadOnlySpan<byte> data, ISerializationRegistry registry)
    {
        Id = EntityId.From(data);
        return 16;
    }
}
