using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Sinks;

namespace NexusMods.EventSourcing;

public class Db(Connection connection, TransactionId basis) : IDb
{
    /// <inheritdoc />
    public TransactionId Basis => basis;


    /// <summary>
    /// Get an entity by its id.
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Get<T>(EntityId<T> id) where T : AEntity
    {
        var sink = new SingleEntityLoader(connection.Registry, this);
        connection.Store.QueryByE(id.Id.Value, ref sink, basis.Value);
        return (T)sink.Entity;
    }

    public IEnumerable<TEntity> Get<TEntity, TValue>(Expression<Func<TEntity, TValue>> query, TValue value) where TEntity : AEntity
    {
        throw new NotImplementedException();
    }
}
