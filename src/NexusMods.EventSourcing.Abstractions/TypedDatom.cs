using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A typed datom for writing new datoms to the database. This is implemented by attributes
/// to provide a typed, unboxed, way of passing datoms through various functions.
/// </summary>
public interface IWriteDatom
{
    /// <summary>
    /// Appends the datom to the given chunk, using the given registry to resolve the attribute
    /// </summary>
    /// <param name="registry"></param>
    /// <param name="chunk"></param>
    void Append(IAttributeRegistry registry, IAppendableChunk chunk);
}


/// <summary>
/// Marker interface for a read datom that will contain a TX value.
/// </summary>
public interface IReadDatom
{
    public Type AttributeType { get; }

    public Type ValueType { get; }
}

