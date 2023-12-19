using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("9C6CF87E-9469-4C9E-87AB-6FE7EF331358")]
[MemoryPackable]
public partial record AddCollection(EntityId<Collection> CollectionId, string Name, EntityId<Loadout> LoadoutId, EntityId<Mod>[] Mods) : IEvent
{
    public void Apply<T>(T context) where T : IEventContext
    {
        IEntity.TypeAttribute.New(context, CollectionId);
        Collection._name.Set(context, CollectionId, Name);
        Collection._loadout.Link(context, CollectionId, LoadoutId);
        Collection._mods.AddAll(context, CollectionId, Mods);
        Loadout._collections.Add(context, LoadoutId, CollectionId);
        foreach (var mod in Mods)
        {
            Mod._collection.Link(context, mod, CollectionId);
        }
    }

    public static EntityId<Collection> Create(ITransaction tx, string name, EntityId<Loadout> loadout, params EntityId<Mod>[] mods)
    {
        var id = EntityId<Collection>.NewId();
        tx.Add(new AddCollection(id, name, loadout, mods));
        return id;
    }
}
