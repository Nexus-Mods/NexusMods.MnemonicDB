namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Constants for where IDs start in a packed ulong. In various places we refer to EntityIs, TempIds and TransactionIds
/// all with a ulong. We partition the space so that we can tell them apart, and do simple comparisons.
/// </summary>
public static class Ids
{
    /// <summary>
    /// The start of the transaction id space.
    /// </summary>
    public const ulong TxStart = 0;

    /// <summary>
    /// The start of the temporary id space.
    /// </summary>
    public const ulong TempIdStart = 0x0100_0000_0000_0000;

    /// <summary>
    /// The start of the entity id space.
    /// </summary>
    public const ulong EntityIdStart = 0x0200_0000_0000_0000;


    /// <summary>
    /// Is the given id a temporary entity id?
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static bool IsTempId(ulong id) => id >= TempIdStart && id < EntityIdStart;

    /// <summary>
    /// Is the given id a entity id?
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static bool IsEntityId(ulong id) => id >= EntityIdStart;

    /// <summary>
    /// Is the given id a transaction id?
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static bool IsTxId(ulong id) => id < TempIdStart;

    /// <summary>
    /// A enum to represent the type of id.
    /// </summary>
    public enum IdType
    {
        /// <summary>
        /// A temporary entity id, used during transaction processing.
        /// </summary>
        TempId,

        /// <summary>
        /// A entity id, used to represent a real entity.
        /// </summary>
        EntityId,

        /// <summary>
        /// A transaction id, used to represent a transaction.
        /// </summary>
        TxId
    }

    /// <summary>
    /// Get the type of the given id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static IdType GetIdType(ulong id) => id switch
    {
        < TempIdStart => IdType.TxId,
        < EntityIdStart => IdType.TempId,
        _ => IdType.EntityId
    };
}
