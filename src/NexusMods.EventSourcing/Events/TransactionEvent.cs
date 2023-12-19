using MemoryPack;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Events;

/// <summary>
/// An aggregate event that groups together a set of events that should be applied together.
/// </summary>
[EventId("DFEC36C4-ACAB-405D-AAEE-2F6348BA108F")]
[MemoryPackable]
public partial record TransactionEvent(IEvent[] Events) : IEvent
{
    /// <inheritdoc />
    public void Apply<T>(T context) where T : IEventContext
    {
        foreach (var evt in Events)
            evt.Apply(context);
    }
}
