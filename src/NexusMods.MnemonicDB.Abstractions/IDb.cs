using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

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
        where TModel : IEntity;

    /// <summary>
    /// Gets a single attribute value for the given entity id.
    /// </summary>
    public TValue Get<TAttribute, TValue>(EntityId id)
        where TAttribute : IAttribute<TValue>;


    /// <summary>
    ///     Gets a read model for the given entity id.
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="TModel"></typeparam>
    /// <returns></returns>
    public TModel Get<TModel>(EntityId id)
        where TModel : IEntity;

    /// <summary>
    ///     Gets a read model for every enitity that references the given entity id
    ///     with the given attribute.
    /// </summary>
    public Entities<EntityIds, TModel> GetReverse<TAttribute, TModel>(EntityId id)
        where TModel : IEntity
        where TAttribute : IAttribute<EntityId>;

    public IEnumerable<IReadDatom> Datoms(EntityId id);

    /// <summary>
    ///     Gets the datoms for the given transaction id.
    /// </summary>
    public IEnumerable<IReadDatom> Datoms(TxId txId);

    /// <summary>
    /// Gets all values for the given attribute on the given entity. There's no reason to use this
    /// on attributes that are not multi-valued.
    /// </summary>
    IEnumerable<TValueType> GetAll<TAttribute, TValueType>(EntityId modelId)
        where TAttribute : IAttribute<TValueType>;


    /// <summary>
    /// Finds all the entity ids that have the given attribute with the given value.
    /// </summary>
    IEnumerable<EntityId> FindIndexed<TAttribute, TValue>(TValue value)
        where TAttribute : IAttribute<TValue>;

    /// <summary>
    /// Finds all the entity ids that have the given attribute.
    /// </summary>
    IEnumerable<EntityId> Find<TAttribute>()
        where TAttribute : IAttribute;


    /// <summary>
    /// Gets the index segment for the given entity id.
    /// </summary>
    IndexSegment GetSegment(EntityId id);
}
