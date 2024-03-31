using System.Text.Json.Serialization;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;
using FileAttributes = NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.ReadModels;

public struct Mod(ModelHeader header) : IEntity
{
    public Mod(ITransaction tx) : this(tx.New()) { }

    public ModelHeader Header { get => header; set => header = value; }


    public string Name
    {
        get => ModAttributes.Name.Get(ref header);
        init => ModAttributes.Name.Add(ref header, value);
    }

    public Uri Source
    {
        get => ModAttributes.Source.Get(ref header);
        init => ModAttributes.Source.Add(ref header, value);
    }

    public EntityId LoadoutId
    {
        get => ModAttributes.LoadoutId.Get(ref header);
        init => ModAttributes.LoadoutId.Add(ref header, value);
    }

    public Loadout Loadout
    {
        get => header.Db.Get<Loadout>(LoadoutId);
        init => ModAttributes.LoadoutId.Add(ref header, value.Id);
    }

    public IEnumerable<File> Files => header.GetReverse<FileAttributes.ModId, File>();


}
