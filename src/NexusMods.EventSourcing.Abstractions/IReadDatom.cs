using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Marker interface for a read datom that will contain a TX value.
/// </summary>
public interface IReadDatom
{
    public Type AttributeType { get; }

    public Type ValueType { get; }
}