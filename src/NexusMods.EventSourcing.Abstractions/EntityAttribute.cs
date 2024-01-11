using System;
using System.Buffers.Binary;

namespace NexusMods.EventSourcing.Abstractions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class EntityAttribute : Attribute
{
    /// <summary>
    /// Defines the UID of the entity, and what revision it is. Incrementing
    /// the revision will cause the entity snapshots to be discarded, and regenerated
    /// during the next load.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="revision"></param>
    public EntityAttribute(string guid, ushort revision)
    {
        Span<byte> span = stackalloc byte[16];
        Guid.Parse(guid).TryWriteBytes(span);
        UUID = BinaryPrimitives.ReadUInt128BigEndian(span);
        Revision = revision;
    }

    /// <summary>
    /// The revision of the entity *Type*.
    /// </summary>
    public ushort Revision { get; }

    /// <summary>
    /// The unique identifier of the entity *Type*.
    /// </summary>
    public UInt128 UUID { get; }
}
