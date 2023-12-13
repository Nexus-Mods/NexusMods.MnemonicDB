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
    /// <param name="event"></param>
    /// <returns></returns>
    public void Ingest(IEvent @event);
}
