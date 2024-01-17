using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("95C3D0AF-EBCA-4DA8-ACAE-144E66F48A50")]
[MemoryPackable]
public partial record RenameLoadout(EntityId<Loadout> Id, string Name) : IEvent
{
    public void Apply<T>(T context) where T : IEventContext
    {
        Loadout._name.Set(context, Id, Name);
        LoadoutRegistry._loadoutNames.Add(context, LoadoutRegistry.SingletonId, Name, Id);
    }

    public static void Create(ITransaction tx, EntityId<Loadout> id, string name)
    {
        tx.Add(new RenameLoadout(id, name));
    }
}
