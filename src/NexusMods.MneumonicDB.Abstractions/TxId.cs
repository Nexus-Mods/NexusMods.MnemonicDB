using TransparentValueObjects;

namespace NexusMods.MneumonicDB.Abstractions;

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
    public static TxId MinValue = From(Ids.MakeId(Ids.Partition.Tx, 0));

    /// <summary>
    ///     The minimum possible value for a TxId after the database has been bootstrapped.
    /// </summary>
    public static TxId MinValueAfterBootstrap = From(Ids.MakeId(Ids.Partition.Tx, 1));

    /// <summary>
    ///     The temporary transaction id, used for referencing the transaction entity
    /// </summary>
    public static TxId Tmp => From(Ids.MakeId(Ids.Partition.Tmp, 0));
}
