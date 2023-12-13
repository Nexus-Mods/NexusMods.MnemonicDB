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
    void Apply<T>(T context) where T : IEventContext;
}
