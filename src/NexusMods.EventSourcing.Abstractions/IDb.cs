using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions.Models;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents an immutable database fixed to a specific TxId.
/// </summary>
public interface IDb : IDisposable
{
    /// <summary>
    /// Gets the basis TxId of the database.
    /// </summary>
    TxId BasisTxId { get; }

    /// <summary>
    /// The connection that this database is using for its state.
    /// </summary>
    IConnection Connection { get; }

    /// <summary>
    /// Returns a read model for each of the given entity ids.
    /// </summary>
    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids)
        where TModel : IReadModel;


    /// <summary>
    /// Gets a read model for the given entity id.
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="TModel"></typeparam>
    /// <returns></returns>
    public TModel Get<TModel>(EntityId id)
        where TModel : IReadModel;

    /// <summary>
    /// Gets a read model for every enitity that references the given entity id
    /// with the given attribute.
    /// </summary>
    public IEnumerable<TModel> GetReverse<TAttribute, TModel>(EntityId id)
        where TModel : IReadModel
        where TAttribute : IAttribute<EntityId>;

    /// <summary>
    /// Reloads the active read model with the latest state from the database.
    /// </summary>
    void Reload<TOuter>(TOuter aActiveReadModel) where TOuter : IActiveReadModel;
}
