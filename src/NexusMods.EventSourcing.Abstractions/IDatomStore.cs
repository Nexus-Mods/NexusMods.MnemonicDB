using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
    public Task<DatomStoreTransactResult> Transact(IEnumerable<IWriteDatom> datoms);

    /// <summary>
    /// An observable of the transaction log, for getting the latest changes to the store.
    /// </summary>
    public IObservable<(TxId TxId, IReadDatom[] Datoms)> TxLog { get; }

    /// <summary>
    /// Gets the latest transaction id found in the log.
    /// </summary>
    public TxId AsOfTxId { get; }

    /// <summary>
    /// Returns all the most recent datoms (less than or equal to txId) with the given attribute.
    /// </summary>
    IEnumerable<Datom> Where<TAttr>(TxId txId) where TAttr : IAttribute;



    /// <summary>
    /// Returns all the most recent datoms (less than or equal to txId) with the given attribute.
    /// </summary>
    IEnumerable<Datom> Where(TxId txId, EntityId id);

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
    /// Gets the attributeId for the given attribute. And returns an expression that reads the attribute
    /// value from the expression valueSpan.
    /// </summary>
    Expression GetValueReadExpression(Type attribute, Expression valueSpan, out AttributeId attributeId);

    /// <summary>
    /// Gets all the entities that reference the given entity id with the given attribute.
    /// </summary>
    IEnumerable<EntityId> ReverseLookup<TAttribute>(TxId txId, EntityId id) where TAttribute : IAttribute<EntityId>;

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
    IEnumerable<EntityId> GetEntitiesWithAttribute<TAttribute>() where TAttribute : IAttribute;

    /// <summary>
    /// Gets all the attributes for the given entity id where the transaction id is less than or equal to the given txId.
    /// </summary>
    IEnumerable<IReadDatom> GetAttributesForEntity(EntityId realId, TxId txId);

    /// <summary>
    /// Gets the maximum entity id in the store.
    /// </summary>
    EntityId GetMaxEntityId();
}
