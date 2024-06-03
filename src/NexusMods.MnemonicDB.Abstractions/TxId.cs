using TransparentValueObjects;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     A typed identifier for a transaction id, internally this is a ulong.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct TxId
{
    /// <summary>
    ///     Maximum possible value for a TxId.
    /// </summary>
    public static TxId MaxValue = From(ulong.MaxValue);

    /// <summary>
    ///     The minimum possible value for a TxId.
    /// </summary>
    public static TxId MinValue = From(PartitionId.Transactions.MakeEntityId(0).Value);

    /// <summary>
    ///     The temporary transaction id, used for referencing the transaction entity
    /// </summary>
    public static TxId Tmp => From(PartitionId.Temp.MakeEntityId(0).Value);

    /// <inheritdoc />
    public override string ToString()
    {
        return "Tx:" + Value.ToString("X");
    }
}
