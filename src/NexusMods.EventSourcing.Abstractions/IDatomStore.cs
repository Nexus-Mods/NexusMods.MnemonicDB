using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NexusMods.EventSourcing.Abstractions.Internals;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents the low-level storage for datoms.
/// </summary>
public interface IDatomStore : IDisposable
{

    /// <summary>
    /// Writes a no-op transaction and waits for it to be processed. This is useful
    /// for making sure that all previous transactions have been processed before continuing.
    /// </summary>
    /// <returns></returns>
    public Task<TxId> Sync();

    /// <summary>
    /// Transacts (adds) the given datoms into the store.
    /// </summary>
    public Task<StoreResult> Transact(IEnumerable<IWriteDatom> datoms);

    /// <summary>
    /// An observable of the transaction log, for getting the latest changes to the store.
    /// </summary>
    public IObservable<(TxId TxId, IReadOnlyCollection<IReadDatom> Datoms)> TxLog { get; }

    /// <summary>
    /// Gets the latest transaction id found in the log.
    /// </summary>
    public TxId AsOfTxId { get; }

    IAttributeRegistry Registry { get; }

    /// <summary>
    /// Resolves the given datoms to typed datoms.
    /// </summary>
    /// <param name="datoms"></param>
    IEnumerable<IReadDatom> Resolved(IEnumerable<Datom> datoms);

    /// <summary>
    /// Registers new attributes with the store. These should already have been transacted into the store.
    /// </summary>
    /// <param name="newAttrs"></param>
    Task RegisterAttributes(IEnumerable<DbAttribute> newAttrs);

    /// <summary>
    /// Gets the entities that have the given attribute that reference the given entity id.
    /// </summary>
    IEnumerable<EntityId> GetReferencesToEntityThroughAttribute<TAttribute>(EntityId id, TxId txId)
        where TAttribute : IAttribute<EntityId>;

    /// <summary>
    /// Gets the value of the given attribute for the given entity id where the transaction id exactly matches the given txId.
    /// </summary>
    bool TryGetExact<TAttr, TValue>(EntityId e, TxId tx, out TValue val) where TAttr : IAttribute<TValue>;

    /// <summary>
    /// Gets the latest value of the given attribute for the given entity id where the transaction id is less than or equal to the given txId.
    /// </summary>
    bool TryGetLatest<TAttribute, TValue>(EntityId e, TxId tx, out TValue value) where TAttribute : IAttribute<TValue>;

    /// <summary>
    /// Gets all the entities that have the given attribute.
    /// </summary>
    IEnumerable<EntityId> GetEntitiesWithAttribute<TAttribute>(TxId tx) where TAttribute : IAttribute;

    /// <summary>
    /// Gets all the attributes for the given entity id where the transaction id is less than or equal to the given txId.
    /// </summary>
    IEnumerable<IReadDatom> GetAttributesForEntity(EntityId realId, TxId txId);

    /// <summary>
    /// Gets the maximum entity id in the store.
    /// </summary>
    EntityId GetMaxEntityId();

    /// <summary>
    /// Gets the type of the read datom for the given attribute.
    /// </summary>
    Type GetReadDatomType(Type attribute);


    /// <summary>
    /// Get all the datoms in a given index, not super useful as this may return a TOOON of datoms.
    /// </summary>
    /// <param name="snapshot"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public IEnumerable<IReadDatom> Datoms(ISnapshot snapshot, IndexType type);

    /// <summary>
    /// Create a snapshot of the current state of the store.
    /// </summary>
    ISnapshot GetSnapshot();
}
