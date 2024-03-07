namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A typed datom for writing new datoms to the database. This is implemented by attributes
/// to provide a typed, unboxed, way of passing datoms through various functions.
/// </summary>
public interface IWriteDatom
{
    /// <summary>
    /// Appends the datom to the given node, using the given registry to resolve the attribute
    /// </summary>
    /// <param name="registry"></param>
    /// <param name="node"></param>
    void Append(IAttributeRegistry registry, IAppendableNode node);
}
