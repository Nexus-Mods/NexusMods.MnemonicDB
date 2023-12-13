using System;
using System.Threading.Tasks;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A interface for a transaction that can be used to add new events to storage.
/// </summary>
public interface ITransaction : IDisposable
{
    /// <summary>
    /// Confirms the transaction and commits the changes to the underlying storage.
    /// </summary>
    /// <returns></returns>
    public ValueTask CommitAsync();

    /// <summary>
    /// Gets the current state of an entity.
    /// </summary>
    /// <param name="entityId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Retrieve<T>(EntityId<T> entityId) where T : IEntity;

    /// <summary>
    /// Adds a new event to the transaction, this will also update the current
    /// entity states
    /// </summary>
    /// <param name="entityId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ValueTask Add<T>(T eventToAdd) where T : IEvent;

}
