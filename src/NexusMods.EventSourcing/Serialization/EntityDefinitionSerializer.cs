using System;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializes an entity definition, while intentionally not serializing the type of the entity.
/// </summary>
public sealed class EntityDefinitionSerializer : IFixedSizeSerializer<EntityDefinition>
{
    /// <inheritdoc />
    public bool CanSerialize(Type valueType)
    {
        return valueType == typeof(EntityDefinition);
    }

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = 16 + sizeof(ushort);
        return true;
    }

    /// <inheritdoc />
    public void Serialize(EntityDefinition value, Span<byte> output)
    {
        BinaryPrimitives.WriteUInt128BigEndian(output, value.UUID);
        BinaryPrimitives.WriteUInt16BigEndian(output.SliceFast(16), value.Revision);
    }

    /// <inheritdoc />
    public EntityDefinition Deserialize(ReadOnlySpan<byte> from)
    {
        var uuid = BinaryPrimitives.ReadUInt128BigEndian(from);
        var revision = BinaryPrimitives.ReadUInt16BigEndian(from.SliceFast(16));

        var existing = EntityStructureRegistry.GetDefinitionByUUID(uuid);

        if (existing.Revision != revision)
            return existing with { Revision = revision };

        return existing;
    }
}
