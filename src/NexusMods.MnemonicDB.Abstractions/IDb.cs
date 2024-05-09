using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Represents an immutable database fixed to a specific TxId.
/// </summary>
public interface IDb : IEquatable<IDb>, IDisposable
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
    ///     Gets a read model for the given entity id.
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="TModel"></typeparam>
    /// <returns></returns>
    public TModel Get<TModel>(EntityId id)
        where TModel : IModel;


    /// <summary>
    /// Gets the index segment for the given entity id.
    /// </summary>
    public IndexSegment Get(EntityId entityId);

    /// <summary>
    ///     Gets a read model for every enitity that references the given entity id
    ///     with the given attribute.
    /// </summary>
    public Entities<EntityIds, TModel> GetReverse<TModel>(EntityId id, Attribute<EntityId, ulong> attribute)
        where TModel : IModel;

    public IEnumerable<IReadDatom> Datoms(EntityId id);

    /// <summary>
    ///     Gets the datoms for the given transaction id.
    /// </summary>
    public IEnumerable<IReadDatom> Datoms(TxId txId);

    /// <summary>
    /// Gets all values for the given attribute on the given entity. There's no reason to use this
    /// on attributes that are not multi-valued.
    /// </summary>
    IEnumerable<TValueType> GetAll<TValueType, TLowLevel>(EntityId modelId, Attribute<TValueType, TLowLevel> attribute);

    /// <summary>
    /// Finds all the entity ids that have the given attribute with the given value.
    /// </summary>
    IEnumerable<EntityId> FindIndexed<TValue, TLowLevel>(TValue value, Attribute<TValue, TLowLevel> attribute);

    /// <summary>
    /// Finds all the datoms have the given attribute with the given value.
    /// </summary>
    IEnumerable<Datom> FindIndexedDatoms<TValue, TLowLevel>(TValue value, Attribute<TValue, TLowLevel> attribute);

    /// <summary>
    /// Finds all the entity ids that have the given attribute.
    /// </summary>
    IEnumerable<EntityId> Find(IAttribute attribute);
}
