using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.TestModel;

public static class Collection
{
    public const string Namespace = "NexusMods.MnemonicDB.TestModel.Collection";

    public static readonly StringAttribute Name = new(Namespace, "Name");
    public static readonly ReferencesAttribute Mods = new(Namespace, "Mods");
    public static readonly ReferenceAttribute Loadout = new(Namespace, "Loadout");


    public class Model(ITransaction tx) : Entity(tx)
    {
        public string Name
        {
            get => Collection.Name.Get(this);
            init => Collection.Name.Add(this, value);
        }

        public IEnumerable<EntityId> ModIds => Collection.Mods.Get(this);

        public Model Attach(Mod.Model mod)
        {
            Collection.Mods.Add(this, mod.Id);
            return this;
        }

        public Entities<Values<EntityId, ulong>, Mod.Model> Mods => Collection.Mods.Get(this).As<Mod.Model>(Db);

        public EntityId LoadoutId
        {
            get => Collection.Loadout.Get(this);
            init => Collection.Loadout.Add(this, value);
        }

        public Loadout.Model Loadout
        {
            get => Db.Get<TestModel.Loadout.Model>(LoadoutId);
            init => Collection.Loadout.Add(this, value.Id);
        }

    }


}
