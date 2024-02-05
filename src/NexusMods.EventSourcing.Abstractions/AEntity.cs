namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Abstract class for an entity
/// </summary>
public abstract class AEntity
{
    /// <summary>
    /// Abstract class for an entity
    /// </summary>
    /// <param name="context"></param>
    /// <param name="id"></param>
    protected AEntity(IDb context, EntityId id)
    {
        Id = id;
        Context = context;
    }

    /// <summary>
    /// Constructor for creating a new entity with a temporary id
    /// </summary>
    /// <param name="tx"></param>
    protected AEntity(Transaction tx)
    {
        Id = tx.TempId();
        tx.Attach(this);
        Context = null!;
    }

    /// <summary>
    /// The unique identifier for this entity
    /// </summary>
    public EntityId Id { get; }

    /// <summary>
    /// The database context this entity is associated with
    /// </summary>
    public IDb Context { get; }

}
