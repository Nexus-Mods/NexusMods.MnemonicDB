using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using FileAttributes = NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.ReadModels;

public struct File(ModelHeader header) : IEntity
{
    public File(ITransaction tx) : this(tx.New()) { }
    public ModelHeader Header { get => header; set => header = value; }

    /// <summary>
    /// The path of the file
    /// </summary>
    public RelativePath Path
    {
        get => FileAttributes.Path.Get(ref header);
        init => FileAttributes.Path.Add(ref header, value);
    }

    /// <summary>
    /// The xxHash64 hash of the file
    /// </summary>
    public Hash Hash
    {
        get => FileAttributes.Hash.Get(ref header);
        init => FileAttributes.Hash.Add(ref header, value);
    }

    /// <summary>
    /// The size of the file
    /// </summary>
    public Size Size
    {
        get => FileAttributes.Size.Get(ref header);
        init => FileAttributes.Size.Add(ref header, value);
    }

    /// <summary>
    /// The id of the mod this file belongs to
    /// </summary>
    public EntityId ModId
    {
        get => FileAttributes.ModId.Get(ref header);
        init => FileAttributes.ModId.Add(ref header, value);
    }

    /// <summary>
    /// The mod this file belongs to
    /// </summary>
    public Mod Mod
    {
        get => header.Db.Get<Mod>(ModId);
        init => FileAttributes.ModId.Add(ref header, value.Header.Id);
    }
}
