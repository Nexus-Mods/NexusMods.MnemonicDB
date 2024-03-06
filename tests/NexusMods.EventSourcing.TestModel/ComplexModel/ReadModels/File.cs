using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.TestModel.ComplexModel.ReadModels;

public class File(ITransaction? tx) : AReadModel<File>(tx)
{
    [From<ComplexModel.Attributes.FileAttributes.Path>]
    public required RelativePath Name { get; set; }

    [From<ComplexModel.Attributes.FileAttributes.Size>]
    public required Size Size { get; set; }

    [From<ComplexModel.Attributes.FileAttributes.Hash>]
    public required Hash Hash { get; set; }

    [From<ComplexModel.Attributes.FileAttributes.ModId>]
    public required EntityId ModId { get; init; }

    public static File Create(ITransaction tx, string s, Mod mod, Size fromLong, Hash hash)
    {
        return new File(tx)
        {
            Name = s,
            Size = fromLong,
            Hash = hash,
            ModId = mod.Id
        };
    }
}

