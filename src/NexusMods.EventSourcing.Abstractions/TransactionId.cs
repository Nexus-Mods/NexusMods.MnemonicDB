using System;
using System.Buffers.Binary;
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

    /// <summary>
    /// Write the transaction id to the given span.
    /// </summary>
    /// <param name="span"></param>
    public void WriteTo(Span<byte> span)
    {
        BinaryPrimitives.WriteUInt64BigEndian(span, Value);
    }

    /// <summary>
    /// Read a transaction id from the given span.
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public static TransactionId From(ReadOnlySpan<byte> span)
    {
        return new(BinaryPrimitives.ReadUInt64BigEndian(span));
    }
}
