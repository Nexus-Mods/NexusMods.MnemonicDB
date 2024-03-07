// ReSharper disable InconsistentNaming
namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Common sort orders for datoms.
/// </summary>
public enum SortOrders : byte
{
    /// <summary>
    /// TX log order - TEAV
    /// </summary>
    TxLog,

    /// <summary>
    /// Common index for looking up all datoms for an entity
    /// </summary>
    EATV,

    /// <summary>
    /// Index for looking up all entities that have a given attribute
    /// </summary>
    AETV,

    /// <summary>
    /// Index for looking up all entities that have a given value for a given attribute
    /// </summary>
    AVTE,
}
