using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using FileAttributes = NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MnemonicDB.TestModel.ComplexModel.ReadModels;

/// <summary>
/// Read model the demonstrates the use of multiple attributes with the same short name (but unique full name).
/// Also demonstrates the use of a read model containing attributes from different attribute classes.
/// </summary>
public class ArchiveFile(ITransaction tx) : AEntity(tx)
{
    public RelativePath Path
    {
        get => FileAttributes.Path.Get(this);
        init => FileAttributes.Path.Add(this, value);
    }

    public Size Size
    {
        get => FileAttributes.Size.Get(this);
        init => FileAttributes.Size.Add(this, value);
    }


    public RelativePath ArchivePath
    {
        get => ArchiveFileAttributes.Path.Get(this);
        init => ArchiveFileAttributes.Path.Add(this, value);
    }


    public Hash ArchiveHash
    {
        get => ArchiveFileAttributes.Hash.Get(this);
        init => ArchiveFileAttributes.Hash.Add(this, value);
    }
}
