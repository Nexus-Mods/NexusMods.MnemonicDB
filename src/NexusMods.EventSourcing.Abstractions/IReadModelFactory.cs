using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A factory for creating read models. The attribute list is used to optimize
/// reading so that only the requested attributes have to be loaded from the store.
/// </summary>
public interface IReadModelFactory
{
    public Type ModelType { get; }

    /// <summary>
    /// A collection of all the attributes in the model.
    /// </summary>
    public Type[] Attributes { get; }

    /// <summary>
    /// Creates a new instance of the read model
    /// </summary>
    /// <returns></returns>
    public IReadModel Create(EntityId id);
}
