namespace NexusMods.MneumonicDB.Abstractions.Models;

public interface IEntity
{
    /// <summary>
    /// The database this entity is associated with.
    /// </summary>
    public IDb Db { get; }

    /// <summary>
    /// The identifier for the entity.
    /// </summary>
    public EntityId Id { get; }


    public TEntity Get<TEntity>(EntityId entityId)
        where TEntity : IEntity
    {
        return Db.Get<TEntity>(entityId);
    }
}
