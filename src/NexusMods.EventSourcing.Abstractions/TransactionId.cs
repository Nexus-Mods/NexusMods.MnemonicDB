using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A transaction id. In the context of this library, a transaction is a set of events that are applied to the entities
/// that they reference. The transaction id is a monotonic increasing number that is used to order the events over an
/// abstract idea of time. Transaction X is always considered to have happened before transaction X + 1.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct TransactionId
{

    /// <summary>
    /// Get the next transaction id.
    /// </summary>
    /// <returns></returns>
    public TransactionId Next() => new(Value + 1);
}
