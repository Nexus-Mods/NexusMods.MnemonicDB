using System;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A factory for creating read models. The attribute list is used to optimize
/// reading so that only the requested attributes have to be loaded from the store.
/// </summary>
public interface IReadModelFactory
{
    /// <summary>
    /// A collection of all the attributes in the model.
    /// </summary>
    public Type[] ReferencedAttributes { get; }

    /// <summary>
    /// the model type this factory is for
    /// </summary>
    public Type ForType { get; }

    /// <summary>
    /// Creates a builder for the given read model
    /// </summary>
    /// <returns></returns>
    public IReadModelBuilder CreateBuilder();
}
