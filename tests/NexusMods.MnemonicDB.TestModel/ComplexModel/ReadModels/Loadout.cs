using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel.ComplexModel.ReadModels;

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

    public IEnumerable<Collection> Collections => header.GetReverse<CollectionAttributes.LoadoutId, Collection>();
}
