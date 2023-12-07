using MemoryPack;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Tests.DataObjects;


public class CountedEntity : IEntity
{
    public string Name { get; internal set; } = "";

    public int Count { get; internal set; } = 0;
    public EntityId Id { get; }
}
