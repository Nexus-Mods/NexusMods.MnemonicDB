using System;
using System.Buffers.Binary;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Marks a class as an entity, and sets the UUID and revision of the entity.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class EntityAttribute : Attribute
{
    /// <summary>
    /// Defines the UID of the entity, and what revision it is. Incrementing
    /// the revision will cause the entity snapshots to be discarded, and regenerated
    /// during the next load.
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="revision"></param>
    public EntityAttribute(string guid, ushort revision = 0)
    {
        Span<byte> span = stackalloc byte[16];
        Guid.Parse(guid).TryWriteBytes(span);
        Uuid = BinaryPrimitives.ReadUInt128BigEndian(span);
        Revision = revision;
    }

    /// <summary>
    /// The revision of the entity *Type*.
    /// </summary>
    public ushort Revision { get; }

    /// <summary>
    /// The unique identifier of the entity *Type*.
    /// </summary>
    public UInt128 Uuid { get; }
}
