using System;
using System.Threading.Tasks;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A mostly internal interface that is used to ingest events from the event store.
/// </summary>
public interface IEventIngester
{
    /// <summary>
    /// Ingests the given event into the context.
    /// </summary>
    /// <param name="id">The transaction id of the event</param>
    /// <param name="event">The event</param>
    /// <returns>false if playback of events should be stopped, true if the next event should be read</returns>
    public bool Ingest(TransactionId id, IEvent @event);
}
