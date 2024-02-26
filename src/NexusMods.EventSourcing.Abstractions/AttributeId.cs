using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A unique identifier for an attribute, also an EntityId.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct AttributeId;
