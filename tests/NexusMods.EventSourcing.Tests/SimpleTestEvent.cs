using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Tests;

[EventId("FAFAB115-045D-4CEC-89E4-1B621FD4FCD8")]
public record SimpleTestEvent(uint A, byte B) : IEvent
{
    public void Apply<T>(T context) where T : IEventContext
    {
        throw new NotImplementedException();
    }
}
