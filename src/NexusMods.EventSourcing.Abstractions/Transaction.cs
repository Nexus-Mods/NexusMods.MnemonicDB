using System;
using System.Collections.Concurrent;
using System.Threading;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents a set of changes to the data source
/// </summary>
public class Transaction : IDisposable
{
    private ulong _nextId = Ids.MinId(IdSpace.Temp);
    private ConcurrentDictionary<EntityId, AEntity> _attachedEntities = new();
    private ConcurrentStack<(ulong E, ulong A, object v)> _changes = new();
    private readonly IConnection _connection;

    internal Transaction(IConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Creates a new temp Id that is unique within the transaction.
    /// </summary>
    /// <returns></returns>
    public EntityId TempId()
    {
        return EntityId.From(Interlocked.Increment(ref _nextId));
    }

    /// <summary>
    /// Adds an entity to the transaction, should only be called during
    /// the construction of a new entity
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="T"></typeparam>
    internal void Attach<T>(T entity) where T : AEntity
    {
        _attachedEntities.TryAdd(entity.Id, entity);
    }

    /// <summary>
    /// Commits the changes to the data source, returns the transaction id
    /// </summary>
    public TransactionId Commit()
    {
        return _connection.Commit(this);
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}
