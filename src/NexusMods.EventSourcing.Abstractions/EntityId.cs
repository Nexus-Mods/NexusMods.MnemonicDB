using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A unique identifier for an entity.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct EntityId;
