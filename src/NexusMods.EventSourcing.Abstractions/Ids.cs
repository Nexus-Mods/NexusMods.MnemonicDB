namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Entity Ids are 64 bit unsigned integers. The high byte is used to store the type of id, and the rest of the bytes are used to store the id.
/// all data is stored in the same format, but we use the high byte to partition the space so we can have several sets of monotonic ids.
/// </summary>
public enum IdSpace : byte
{
    /// <summary>
    /// Stores attribute definitions
    /// </summary>
    Attr,
    /// <summary>
    /// Temporary space used in transaction processing
    /// </summary>
    Temp,
    /// <summary>
    /// Transaction metadata
    /// </summary>
    Tx,
    /// <summary>
    /// The user space for entity ids
    /// </summary>
    Entity,
}

/// <summary>
/// Constants for where IDs start in a packed ulong. In various places we refer to EntityIs, TempIds and TransactionIds
/// all with a ulong. We partition the space so that we can tell them apart, and do simple comparisons.
/// </summary>
public static class Ids
{
    /// <summary>
    /// Returns true if the id is in the space
    /// </summary>
    /// <param name="space"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static bool IsIdOfSpace(ulong id, IdSpace space) => (id >> 56) == (byte)space;

    /// <summary>
    /// The maximum id for the space
    /// </summary>
    /// <param name="space"></param>
    /// <returns></returns>
    public static ulong MaxId(IdSpace space) => (ulong)space << 56 | 0xFFFFFFFFFFFFFF;

    /// <summary>
    /// The minimum id for the space
    /// </summary>
    /// <param name="space"></param>
    /// <returns></returns>
    public static ulong MinId(IdSpace space) => (ulong)space << 56;
}
