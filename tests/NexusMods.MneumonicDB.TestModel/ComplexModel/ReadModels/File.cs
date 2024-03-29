using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using FileAttributes = NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.ReadModels;

public class File(ITransaction? tx) : AReadModel<File>(tx)
{
    /// <summary>
    /// The path of the file
    /// </summary>
    public RelativePath Path
    {
        get => FileAttributes.Path.Get(this);
        init => FileAttributes.Path.Add(this, value);
    }

    /// <summary>
    /// The size of the file
    /// </summary>
    public Size Size
    {
        get => FileAttributes.Size.Get(this);
        init => FileAttributes.Size.Add(this, value);
    }

    /// <summary>
    /// The hashcode of the file
    /// </summary>
    public Hash Hash
    {
        get => FileAttributes.Hash.Get(this);
        init => FileAttributes.Hash.Add(this, value);
    }


    /// <summary>
    /// The id of the mod this file belongs to
    /// </summary>
    public EntityId ModId
    {
        get => FileAttributes.ModId.Get(this);
        init => FileAttributes.ModId.Add(this, value);
    }

    /// <summary>
    /// The mod this file belongs to
    /// </summary>
    public Mod Mod => Get<Mod>(ModId);


    public static File Create(ITransaction tx, string s, Mod mod, Size fromLong, Hash hash)
    {
        return new File(tx)
        {
            Path = s,
            Size = fromLong,
            Hash = hash,
            ModId = mod.Id
        };
    }
}
