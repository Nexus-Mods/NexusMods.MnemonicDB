using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Marker interface for a read datom that will contain a TX value.
/// </summary>
public interface IReadDatom
{
    /// <summary>
    /// The C# type of the attribute.
    /// </summary>
    public Type AttributeType { get; }

    /// <summary>
    /// The value type of the datom, this is used to find the correct serializer.
    /// </summary>
    public Type ValueType { get; }
}
