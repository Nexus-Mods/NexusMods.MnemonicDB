﻿using NexusMods.MneumonicDB.Abstractions;
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
public class ArchiveFile(ITransaction? tx) : AReadModel<File>(tx)
{
    [From<FileAttributes.Path>]
    public required RelativePath Path { get; set; }

    [From<FileAttributes.Size>]
    public required Size Size { get; set; }

    [From<ArchiveFileAttributes.Path>]
    public required RelativePath ArchivePath { get; set; }

    [From<ArchiveFileAttributes.Hash>]
    public required Hash Hash { get; set; }
}
