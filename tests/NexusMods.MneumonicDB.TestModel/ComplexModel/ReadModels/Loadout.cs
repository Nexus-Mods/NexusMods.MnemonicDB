using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.ReadModels;

public struct Loadout(ModelHeader header) : IEntity
{
    public EntityId Id => header.Id;

    public Loadout(ITransaction tx) : this(tx.New()) { }
    public ModelHeader Header { get => header; set => header = value; }


    public string Name
    {
        get => LoadoutAttributes.Name.Get(ref header);
        init => LoadoutAttributes.Name.Add(ref header, value);
    }

    public IEnumerable<Mod> Mods => header.GetReverse<ModAttributes.LoadoutId, Mod>();
}
