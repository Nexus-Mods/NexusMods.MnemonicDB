using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

public record TransactionResult(IDb newDb, IDictionary<ulong, ulong> EntityRemaps)
{
    /// <summary>
    /// Gets an entity by its id, remapping the id if necessary
    /// </summary>
    /// <param name="oldId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Get<T>(EntityId oldId) where T : AEntity
    {
        if (EntityRemaps.TryGetValue(oldId.Value, out var newId))
        {
            return newDb.Get(EntityId<T>.From(newId));
        }

        return newDb.Get(EntityId<T>.From(oldId.Value));
    }

    /// <summary>
    /// Reloads the entity from the new database
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Refresh<T>(T entity) where T : AEntity
    {
        return Get<T>(entity.Id);
    }

    /// <summary>
    /// The new database
    /// </summary>
    public IDb NewDb => newDb;
}
