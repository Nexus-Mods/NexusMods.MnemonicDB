using System;
using System.Threading.Tasks;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A mostly internal interface that is used to ingest events from the event store.
/// </summary>
public interface IEventIngester
{
    public ValueTask Ingest(IEvent @event);
}
