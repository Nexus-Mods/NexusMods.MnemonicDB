using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using FileAttributes = NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MnemonicDB.TestModel.ComplexModel.ReadModels;

public class File : AEntity
{
    public File(ITransaction tx) : base(tx) { }

    public File(EntityId id, IDb db) : base(id, db) { }

    /// <summary>
    /// The path of the file
    /// </summary>
    public RelativePath Path
    {
        get => FileAttributes.Path.Get(this);
        init => FileAttributes.Path.Add(this, value);
    }

    /// <summary>
    /// The xxHash64 hash of the file
    /// </summary>
    public Hash Hash
    {
        get => FileAttributes.Hash.Get(this);
        init => FileAttributes.Hash.Add(this, value);
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
    public Mod Mod
    {
        get => Db.Get<Mod>(ModId);
        init => FileAttributes.ModId.Add(this, value.Id);
    }
}
