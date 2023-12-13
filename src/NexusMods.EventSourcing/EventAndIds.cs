using MemoryPack;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

/// <summary>
/// A pair of an event and the entity ids it applies to.
/// </summary>
[MemoryPackable]
public class EventAndIds
{
    /// <summary>
    /// The event
    /// </summary>
    public required IEvent Event { get; init; }

    /// <summary>
    /// The entities retrieved by the event
    /// </summary>
    public required EntityId[] EntityIds { get; init; }
}
