using System.Runtime.Serialization;
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

        //Emit(Id, "Name", Name);
        // Loadout_Name(id, Name);


        LoadoutRegistry._loadouts.Add(context, LoadoutRegistry.TypedSingletonId, Id);
        LoadoutRegistry._loadoutNames.Add(context, LoadoutRegistry.TypedSingletonId, Name, Id);


        // [Id, *, ID2]
        // [FileID, "Mod", ModId, TX]
        // [TX, "Reason, "Added SMIM", TX]
        // Mod._files = datoms.Where(d => attr == "Mod" && val == ModId).Select(d => d.ent).ToList();



        //

        // select E, A, arg_max(V, T) VAL, arg_max(T, T) FROM DATOMS WHERE E = 42 GROUP BY E, A

        // LoadoutID vs DatastoreID
        // LoadoutID as of TX


        //


    }

    public static EntityId<Loadout> Create(ITransaction tx, string name)
    {
        var id = EntityId<Loadout>.NewId();
        tx.Add(new CreateLoadout(id, name));
        return id;
    }
}
