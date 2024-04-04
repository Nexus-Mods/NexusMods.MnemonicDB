using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes;
using FileAttributes = NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MnemonicDB.TestModel.ComplexModel.ReadModels;

public class Mod : AEntity
{

    public Mod(ITransaction tx) : base(tx) { }

    public Mod(EntityId id, IDb db) : base(id, db) { }

    public static IEntity Create(EntityId id, IDb db) => new Mod(id, db);


    public string Name
    {
        get => ModAttributes.Name.Get(this);
        init => ModAttributes.Name.Add(this, value);
    }

    public Uri Source
    {
        get => ModAttributes.Source.Get(this);
        init => ModAttributes.Source.Add(this, value);
    }

    public EntityId LoadoutId
    {
        get => ModAttributes.LoadoutId.Get(this);
        init => ModAttributes.LoadoutId.Add(this, value);
    }

    public Loadout Loadout
    {
        get => Db.Get<Loadout>(LoadoutId);
        init => ModAttributes.LoadoutId.Add(this, value.Id);
    }

    public IEnumerable<File> Files => GetReverse<FileAttributes.ModId, File>();


}
