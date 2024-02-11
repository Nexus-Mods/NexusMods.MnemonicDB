using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents an immutable database fixed to a specific TxId.
/// </summary>
public interface IDb
{
    /// <summary>
    /// Gets the basis TxId of the database.
    /// </summary>
    TxId BasisTxId { get; }

    public IIterator Where<TAttr>()
    where TAttr : IAttribute;

    public IIterator Where(EntityId id);
}
