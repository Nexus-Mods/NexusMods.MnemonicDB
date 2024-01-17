using System;
using System.Buffers.Binary;

namespace NexusMods.EventSourcing.Abstractions;


/// <summary>
/// Marks a static usage of a attribute definition as indexed. When this is specified, the EventStore will
/// mark transactions containing events that use this attribute in a composite index. This allows for fast
/// retrieval of transactions that contain specific key/value pairs. This in turn can be used for fast
/// lookup of entities by their attribute values. See <see cref="IIndexableAttribute"/> for more information.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IndexedAttribute : Attribute
{
    /// <summary>
    /// The unique identifier for the attribute. This is used to to uniquely identify the index of this attribute
    /// </summary>
    public ushort Id { get; set; }

    /// <summary>
    /// Marks a static usage of a attribute definition as indexed. Guid is a unique identifier for the attribute
    /// that will be used in the database. This allows for the attribute to be renamed, without breaking the
    /// internal datamodel
    /// </summary>
    /// <param name="guid"></param>
    public IndexedAttribute(string guid)
    {
        var guidVal = Guid.Parse(guid);
        Span<byte> guidSpan = stackalloc byte[16];
        guidVal.TryWriteBytes(guidSpan);
        Id = BinaryPrimitives.ReadUInt16BigEndian(guidSpan);
    }

}
