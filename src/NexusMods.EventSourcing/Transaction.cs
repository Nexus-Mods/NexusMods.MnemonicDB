using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Events;

namespace NexusMods.EventSourcing;

internal class Transaction(IEntityContext context, List<IEvent> events) : ITransaction
{
    /// <inheritdoc />
    public void Add(IEvent @event)
    {
        events.Add(@event);
    }

    /// <inheritdoc />
    public TransactionId Commit()
    {
        if (events.Count == 1)
            return context.Add(events[0]);
        var id = context.Add(new TransactionEvent(events.ToArray()));
        events.Clear();
        return id;
    }

    public void Dispose()
    {
        events.Clear();
    }
}
