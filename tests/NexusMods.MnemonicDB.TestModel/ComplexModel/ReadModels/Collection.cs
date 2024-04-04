using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel.ComplexModel.ReadModels;

public struct Collection(ModelHeader header) : IEntity
{
    public Collection(ITransaction tx) : this(tx.New()) { }
    public ModelHeader Header { get => header; set => header = value; }

    public string Name
    {
        get => CollectionAttributes.Name.Get(ref header);
        init => CollectionAttributes.Name.Add(ref header, value);
    }

    public IEnumerable<EntityId> ModIds => CollectionAttributes.Mods.GetAll(ref header);

    public Collection Attach(Mod mod)
    {
        CollectionAttributes.Mods.Add(ref header, mod.Header.Id);
        return this;
    }

    public IEnumerable<Mod> Mods => header.Db.Get<Mod>(CollectionAttributes.Mods.GetAll(ref header));

    public EntityId LoadoutId
    {
        get => CollectionAttributes.LoadoutId.Get(ref header);
        init => CollectionAttributes.LoadoutId.Add(ref header, value);
    }

    public Loadout Loadout
    {
        get => header.Db.Get<Loadout>(LoadoutId);
        init => CollectionAttributes.LoadoutId.Add(ref header, value.Id);
    }


}
