using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A context for adding events to an aggregate event that will apply the events together.
/// </summary>
public interface ITransaction : IDisposable
{
    /// <summary>
    /// Adds the event to the transaction, but does not apply it.
    /// </summary>
    /// <param name="event"></param>
    public void Add(IEvent @event);

    /// <summary>
    /// Commits the transaction, applying all events
    /// </summary>
    public TransactionId Commit();
}
