using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel.ComplexModel.ReadModels;

public class Loadout(ITransaction tx) : AEntity(tx)
{
    public string Name
    {
        get => LoadoutAttributes.Name.Get(this);
        init => LoadoutAttributes.Name.Add(this, value);
    }

    public IEnumerable<Mod> Mods => GetReverse<ModAttributes.LoadoutId, Mod>();

    public IEnumerable<Collection> Collections => GetReverse<CollectionAttributes.LoadoutId, Collection>();
}
