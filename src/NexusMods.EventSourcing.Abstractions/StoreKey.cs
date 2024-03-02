using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents a IKVStore key, which is a 64-bit unsigned integer, these make heavy
/// use of integer partitioning to ensure that keys are unique yet still sortable and
/// iterable.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct StoreKey
{
}
