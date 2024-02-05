using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents an immutable database. This database consists of entities all of which are
/// locked to a specific version of the database. Adding datoms to the datastore results in a
/// new version of the database. Entities are only equal if they are the same id from the same
/// version of the database.
/// </summary>
public interface IDb
{
    /// <summary>
    /// The current transaction id of the database
    /// </summary>
    public TransactionId Basis { get; }

    /// <summary>
    /// Gets the entity with the given id from the database
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Get<T>(EntityId<T> id) where T : AEntity;

    /// <summary>
    /// Gets all entities of type T from the database where the given attribute matches the given value. The attribute
    /// is expressed as a lambda expression that selects the attribute from the entity.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="value"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public IEnumerable<TEntity> Get<TEntity, TValue>(Expression<Func<TEntity, TValue>> query, TValue value) where TEntity : AEntity;
}
