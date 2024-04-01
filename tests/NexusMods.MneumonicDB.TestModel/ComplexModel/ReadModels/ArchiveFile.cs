using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using FileAttributes = NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.ReadModels;

/// <summary>
/// Read model the demonstrates the use of multiple attributes with the same short name (but unique full name).
/// Also demonstrates the use of a read model containing attributes from different attribute classes.
/// </summary>
public struct ArchiveFile : IEntity
{
    private ModelHeader _header;

    public ModelHeader Header { get => _header; set => _header = value; }

    public RelativePath Path
    {
        get => FileAttributes.Path.Get(ref _header);
        init => FileAttributes.Path.Add(ref _header, value);
    }

    public Size Size
    {
        get => FileAttributes.Size.Get(ref _header);
        init => FileAttributes.Size.Add(ref _header, value);
    }


    public RelativePath ArchivePath
    {
        get => ArchiveFileAttributes.Path.Get(ref _header);
        init => ArchiveFileAttributes.Path.Add(ref _header, value);
    }


    public Hash ArchiveHash
    {
        get => ArchiveFileAttributes.Hash.Get(ref _header);
        init => ArchiveFileAttributes.Hash.Add(ref _header, value);
    }
}
