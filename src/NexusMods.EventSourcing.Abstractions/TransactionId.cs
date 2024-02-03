using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;


/// <summary>
/// A unique identifier for a transaction.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct TransactionId { }
