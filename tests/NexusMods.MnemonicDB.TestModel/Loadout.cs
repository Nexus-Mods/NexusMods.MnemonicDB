using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.TestModel;

public static class Loadout
{
    public const string Namespace = "NexusMods.MnemonicDB.TestModel.Loadout";
    public static readonly StringAttribute Name = new(Namespace, "Name");


    public class Model(ITransaction tx) : Entity(tx)
    {
        public string Name
        {
            get => Loadout.Name.Get(this);
            init => Loadout.Name.Add(this, value);
        }

        public IEnumerable<Mod.Model> Mods => GetReverse<Mod.Model>(Mod.LoadoutId);

        public IEnumerable<Collection.Model> Collections => GetReverse<Collection.Model>(Collection.Loadout);
    }
}
