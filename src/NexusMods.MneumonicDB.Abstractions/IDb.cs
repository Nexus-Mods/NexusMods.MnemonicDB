using System;
using System.Collections.Generic;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;
using NexusMods.MneumonicDB.Abstractions.Models;

namespace NexusMods.MneumonicDB.Abstractions;

/// <summary>
///     Represents an immutable database fixed to a specific TxId.
/// </summary>
public interface IDb : IDisposable
{
    /// <summary>
    ///     Gets the basis TxId of the database.
    /// </summary>
    TxId BasisTxId { get; }

    /// <summary>
    ///     The connection that this database is using for its state.
    /// </summary>
    IConnection Connection { get; }

    /// <summary>
    /// The snapshot that this database is based on.
    /// </summary>
    ISnapshot Snapshot { get; }

    /// <summary>
    /// The registry that this database is based on.
    /// </summary>
    IAttributeRegistry Registry { get; }

    /// <summary>
    ///     Returns a read model for each of the given entity ids.
    /// </summary>
    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids)
        where TModel : struct, IEntity;

    /// <summary>
    /// Gets a single attribute value for the given entity id.
    /// </summary>
    public TValue Get<TAttribute, TValue>(ref ModelHeader header, EntityId id)
        where TAttribute : IAttribute<TValue>;


    /// <summary>
    ///     Gets a read model for the given entity id.
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="TModel"></typeparam>
    /// <returns></returns>
    public TModel Get<TModel>(EntityId id)
        where TModel : struct, IEntity;

    /// <summary>
    ///     Gets a read model for every enitity that references the given entity id
    ///     with the given attribute.
    /// </summary>
    public TModel[] GetReverse<TAttribute, TModel>(EntityId id)
        where TModel : struct, IEntity
        where TAttribute : IAttribute<EntityId>;

    public IEnumerable<IReadDatom> Datoms(EntityId id);

    /// <summary>
    ///     Gets the datoms for the given transaction id.
    /// </summary>
    public IEnumerable<IReadDatom> Datoms(TxId txId);

    public IEnumerable<IReadDatom> Datoms<TAttribute>()
        where TAttribute : IAttribute;

    /// <summary>
    /// Gets all values for the given attribute on the given entity. There's no reason to use this
    /// on attributes that are not multi-valued.
    /// </summary>
    IEnumerable<TValueType> GetAll<TAttribute, TValueType>(ref ModelHeader model, EntityId modelId)
        where TAttribute : IAttribute<TValueType>;
}
