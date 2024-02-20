using System;
using System.Collections.Generic;

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
    /// Retrieves the read model from the database
    /// </summary>
    /// <param name="tx"></param>
    /// <typeparam name="TReadModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    protected TReadModel Get<TReadModel>(EntityId entityId)
        where TReadModel : AReadModel<TReadModel>, IReadModel
    {
        return Db.Get<TReadModel>(entityId);
    }

    /// <summary>
    /// Retrieves the matching read models from the database via the specified reverse lookup attribute
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <typeparam name="TReadModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    protected IEnumerable<TReadModel> GetReverse<TAttribute, TReadModel>()
        where TReadModel : AReadModel<TReadModel>, IReadModel
        where TAttribute : ScalarAttribute<TAttribute, EntityId>
    {
        return Db.GetReverse<TAttribute, TReadModel>(Id);
    }

    /// <summary>
    /// The base identifier for the entity.
    /// </summary>
    public EntityId Id { get; internal set; }

    /// <summary>
    /// The database this read model is associated with.
    /// </summary>
    public IDb Db { get; internal set; } = null!;
}
