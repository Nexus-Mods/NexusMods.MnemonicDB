using System;
using System.Threading.Tasks;
using MemoryPack;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A single event that can be applied to an entity.
/// </summary>
[MemoryPackable(GenerateType.NoGenerate)]
public interface IEvent
{
    /// <summary>
    /// Applies the event to the entities attached to the event.
    /// </summary>
    ValueTask Apply<T>(T context) where T : IEventContext;

    /// <summary>
    /// When called, the handler should be called for each entity that was modified by this event. Not for
    /// those which are referenced, but not modified.
    /// </summary>
    /// <param name="handler"></param>
    void ModifiedEntities(Action<EntityId> handler);
}
