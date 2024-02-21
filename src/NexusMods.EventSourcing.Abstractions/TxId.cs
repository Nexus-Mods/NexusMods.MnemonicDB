using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A typed identifier for a transaction id, internally this is a ulong.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct TxId
{

    /// <summary>
    /// Maximum possible value for a TxId.
    /// </summary>
    public static TxId MaxValue = From(ulong.MaxValue);
}
