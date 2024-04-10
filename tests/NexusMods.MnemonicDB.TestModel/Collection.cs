using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.TestModel;

public class Collection(ITransaction tx) : AEntity(tx)
{
    public static readonly Attribute<string> Name = new("NexusMods.MnemonicDB.TestModel.Collection/Name");
    public static readonly Attribute<EntityId> Mods = new("NexusMods.MnemonicDB.TestModel.Collection/Mods", cardinality: Cardinality.Many);
    public static readonly Attribute<EntityId> Loadout = new("NexusMods.MnemonicDB.TestModel.Collection/Loadout", cardinality: Cardinality.Many);


    public class Model(ITransaction tx) : AEntity(tx)
    {
        public string Name
        {
            get => Collection.Name.Get(this);
            init => Collection.Name.Add(this, value);
        }

        public Values<EntityId> ModIds => Collection.Mods.GetAll(this);

        public Model Attach(Mod.Model mod)
        {
            Collection.Mods.Add(this, mod.Id);
            return this;
        }

        public Entities<Values<EntityId>, Mod.Model> Mods => Collection.Mods.GetAll(this).As<Mod.Model>(Db);

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
