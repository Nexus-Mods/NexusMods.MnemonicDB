using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Base interface for all factories.
/// </summary>
public interface IRepository
{
    /// <summary>
    /// The type of the model that this factory creates.
    /// </summary>
    public Type RepositoryType { get; }
}

/// <summary>
/// Methods for creating new instances of a model.
/// </summary>
public interface IRepository<T> : IRepository
{
    /// <summary>
    /// Creates a new instance of the model from the given <paramref name="db"/> and <paramref name="id"/>.
    /// </summary>
    public T Create(IDb db, EntityId id);

    /// <summary>
    /// Gets an instance of the model with the given <paramref name="id"/> but also validates the model, returning
    /// false if the model is not valid.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    public bool TryGet(EntityId id, [NotNullWhen(true)] out T model);

    /// <summary>
    /// All the instances of this model in the database.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<T> All();

    /// <summary>
    /// Gets all the instances of this model that have the given <paramref name="value"/> for the given <paramref name="attr"/>.
    /// </summary>
    public IEnumerable<T> Where<THigher, TLower>(Attribute<THigher, TLower> attr, THigher value);
}
