using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;

[ValueObject<ulong>]
public readonly partial struct TxId
{

    /// <summary>
    /// Maximum possible value for a TxId.
    /// </summary>
    public static TxId MaxValue = From(ulong.MaxValue);
}
