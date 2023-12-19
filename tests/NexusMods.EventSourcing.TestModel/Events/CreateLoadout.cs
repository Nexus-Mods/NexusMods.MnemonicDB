using DynamicData;
using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("63A4CB90-27E2-468A-BE94-CB01A38D8C09")]
[MemoryPackable]
public partial record CreateLoadout(EntityId<Loadout> Id, string Name) : IEvent
{
    public void Apply<T>(T context) where T : IEventContext
    {
        IEntity.TypeAttribute.New(context, Id);
        Loadout._name.Set(context, Id, Name);
        LoadoutRegistry._loadouts.Add(context, LoadoutRegistry.SingletonId, Id);
    }
    public static EntityId<Loadout> Create(ITransaction tx, string name)
    {
        var id = EntityId<Loadout>.NewId();
        tx.Add(new CreateLoadout(id, name));
        return id;
    }
}
