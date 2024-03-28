using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using FileAttributes = NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.ReadModels;

public class File(ITransaction? tx) : AReadModel<File>(tx)
{
    [From<FileAttributes.Path>]
    public required RelativePath Path { get; set; }

    [From<FileAttributes.Size>]
    public required Size Size { get; set; }

    [From<FileAttributes.Hash>]
    public required Hash Hash { get; set; }

    [From<FileAttributes.ModId>]
    public required EntityId ModId { get; init; }

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
