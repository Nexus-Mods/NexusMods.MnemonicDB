using System;
using System.Threading.Tasks;
using MemoryPack;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Events;

[MemoryPackable]
public class TransactionEvent : IEvent
{
    /// <summary>
    /// A list of events that are part of the transaction.
    /// </summary>
    public required EventAndIds[] Events { get; init; }

    /// <summary>
    /// Applies all the events in the transaction to the entities attached to the events.
    /// </summary>
    /// <param name="context"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public void Apply<T>(T context) where T : IEventContext
    {
        foreach (var evt in Events)
            evt.Event.Apply(context);
    }
}
