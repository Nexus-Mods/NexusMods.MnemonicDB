using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents the low-level storage for datoms.
/// </summary>
public interface IDatomStore
{

    public TxId Transact(IEnumerable<IDatom> datoms);

    /// <summary>
    /// Returns all the most recent datoms (less than or equal to txId) with the given attribute.
    /// </summary>
    /// <param name="txId"></param>
    /// <typeparam name="TAttr"></typeparam>
    /// <returns></returns>
    IIterator Where<TAttr>(TxId txId) where TAttr : IAttribute;

    /// <summary>
    /// Creates an iterator over all entities.
    /// </summary>
    /// <param name="txId"></param>
    /// <returns></returns>
    IEntityIterator EntityIterator(TxId txId);

}
