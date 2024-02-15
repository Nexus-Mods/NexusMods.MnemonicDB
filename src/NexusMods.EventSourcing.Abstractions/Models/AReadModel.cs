namespace NexusMods.EventSourcing.Abstractions.Models;

/// <summary>
/// Base class for all read models.
/// </summary>
/// <param name="id"></param>
public abstract class AReadModel<TOuter> : IReadModel
where TOuter : AReadModel<TOuter>, IReadModel
{
    /// <summary>
    /// Creates a new read model with a temporary id
    /// </summary>
    /// <param name="tx"></param>
    protected AReadModel(ITransaction? tx)
    {
        if (tx is null) return;
        Id = tx.TempId();
        tx.Add<TOuter>((TOuter)this);
    }

    internal AReadModel(EntityId id)
    {
        Id = id;
    }

    /// <summary>
    /// The base identifier for the entity.
    /// </summary>
    public EntityId Id { get; internal set; }
}
