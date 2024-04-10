using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.TestModel;

public static class Mod
{

    public static readonly Attribute<string> Name = new("NexusMods.MnemonicDB.TestModel.Mod/Name", isIndexed: true);
    public static readonly Attribute<Uri> Source = new("NexusMods.MnemonicDB.TestModel.Mod/Source");
    public static readonly Attribute<EntityId> LoadoutId = new("NexusMods.MnemonicDB.TestModel.Mod/Loadout");

    public class Model(ITransaction tx) : AEntity(tx)
    {
        public string Name
        {
            get => Mod.Name.Get(this);
            init => Mod.Name.Add(this, value);
        }

        public Uri Source
        {
            get => Mod.Source.Get(this);
            init => Mod.Source.Add(this, value);
        }

        public EntityId LoadoutId
        {
            get => Mod.LoadoutId.Get(this);
            init => Mod.LoadoutId.Add(this, value);
        }

        public Loadout.Model Loadout
        {
            get => Db.Get<Loadout.Model>(LoadoutId);
            init => Mod.LoadoutId.Add(this, value.Id);
        }

        public Entities<EntityIds, File.Model> Files => GetReverse<File.Model>(File.ModId);

    }


}
