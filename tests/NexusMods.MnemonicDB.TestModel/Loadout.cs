using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.TestModel;

public class Loadout
{
    public static readonly Attribute<string> Name = new("NexusMods.MnemonicDB.TestModel.Loadout/Name");


    public class Model(ITransaction tx) : AEntity(tx)
    {
        public string Name
        {
            get => Loadout.Name.Get(this);
            init => Loadout.Name.Add(this, value);
        }

        public IEnumerable<Mod.Model> Mods => GetReverse<Mod.Model>(Mod.LoadoutId);

        public IEnumerable<Collection> Collections => GetReverse<Collection>(Collection.Loadout);
    }
}
