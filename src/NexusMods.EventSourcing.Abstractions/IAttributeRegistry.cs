using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A registry of attributes and serializers that supports operations that requires converting
/// between the database IDs, the code-level attributes and the native values
/// </summary>
public interface IAttributeRegistry
{
    /// <summary>
    /// Appends the data to the given node
    /// </summary>
    public void Append<TAttribute, TValue>(IAppendableNode node, EntityId e, TValue value, TxId t, DatomFlags f)
        where TAttribute : IAttribute<TValue>;

    /// <summary>
    /// Compares the given values in the given spans assuming both are tagged with the given attribute
    /// </summary>
    public int CompareValues(AttributeId id, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
