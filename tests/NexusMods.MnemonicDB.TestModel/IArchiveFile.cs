using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel;

/// <summary>
/// Read model the demonstrates the use of multiple attributes with the same short name (but unique full name).
/// Also demonstrates the use of a read model containing attributes from different attribute classes.
/// </summary>
public interface IArchiveFile : IFile
{
    public static class Attributes
    {
        public const string Namespace = "NexusMods.MnemonicDB.TestModel.ArchiveFile";

        public static readonly RelativePathAttribute Path = new(Namespace, "Path");

        public static readonly HashAttribute Hash = new(Namespace, "Hash");
    }

    [From(nameof(Attributes.Path))]
    public new RelativePath Path { get; set; }

    [From(nameof(Attributes.Hash))]
    public new Hash Hash { get; set; }
}
