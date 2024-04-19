using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel;

public static class Mod
{
    private const string Namespace = "NexusMods.MnemonicDB.TestModel.Mod";

    public static readonly StringAttribute Name = new(Namespace, "Name") { IsIndexed = true };
    public static readonly UriAttribute Source = new(Namespace, "Source");
    public static readonly ReferenceAttribute LoadoutId = new(Namespace, "Loadout");

    public class Model(ITransaction tx) : Entity(tx)
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
