using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Abstractions;

public class EntityIdDefinition : IAttribute<EntityDefinitionAccumulator>, IIndexableAttribute<EntityId>
{
    /// <inheritdoc />
    public Type Owner => typeof(IEntity);

    /// <inheritdoc />
    public string Name => "Id";

    /// <inheritdoc />
    public EntityDefinitionAccumulator CreateAccumulator()
    {
        return new EntityDefinitionAccumulator();
    }

    IAccumulator IAttribute.CreateAccumulator()
    {
        return new EntityDefinitionAccumulator();
    }

    /// <inheritdoc />
    public void WriteTo(Span<byte> span, EntityId value)
    {
        BinaryPrimitives.WriteUInt128BigEndian(span, value.Value);
    }

    private static readonly UInt128 IndexAttrId = UInt128.Parse("6a434d5d732e40278d0e43385482368d", NumberStyles.HexNumber);

    /// <inheritdoc />
    public UInt128 IndexedAttributeId
    {
        get => IndexAttrId;
        set => throw new InvalidOperationException("Can't set the indexed attribute id.");
    }

    /// <inheritdoc />
    public int SpanSize()
    {
        return 16;
    }

    /// <inheritdoc />
    public void WriteTo(Span<byte> span, IAccumulator accumulator)
    {
        if (accumulator is not EntityDefinitionAccumulator entityDefinitionAccumulator)
            throw new InvalidOperationException("Invalid accumulator type.");

        BinaryPrimitives.WriteUInt128BigEndian(span, entityDefinitionAccumulator.Id.Value);
    }
}

public class EntityDefinitionAccumulator : IAccumulator
{
    public EntityId Id;

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
