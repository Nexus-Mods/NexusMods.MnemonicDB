using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel;

/// <summary>
/// Read model the demonstrates the use of multiple attributes with the same short name (but unique full name).
/// Also demonstrates the use of a read model containing attributes from different attribute classes.
/// </summary>
public class ArchiveFile
{
    public const string Namespace = "NexusMods.MnemonicDB.TestModel.ArchiveFile";

    public static readonly RelativePathAttribute Path = new(Namespace, "Path");

    public static readonly HashAttribute Hash = new(Namespace, "Hash");



    public class Model(ITransaction tx) : AEntity(tx)
    {
        public RelativePath Path
        {
            get => File.Path.Get(this);
            init => File.Path.Add(this, value);
        }

        public Size Size
        {
            get => File.Size.Get(this);
            init => File.Size.Add(this, value);
        }


        public RelativePath ArchivePath
        {
            get => ArchiveFile.Path.Get(this);
            init => ArchiveFile.Path.Add(this, value);
        }


        public Hash ArchiveHash
        {
            get => ArchiveFile.Hash.Get(this);
            init => ArchiveFile.Hash.Add(this, value);
        }

    }
}
