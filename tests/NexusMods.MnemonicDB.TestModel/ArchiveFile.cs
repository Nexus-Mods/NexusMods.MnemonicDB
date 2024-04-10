using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel;

/// <summary>
/// Read model the demonstrates the use of multiple attributes with the same short name (but unique full name).
/// Also demonstrates the use of a read model containing attributes from different attribute classes.
/// </summary>
public class ArchiveFile
{

    public static readonly Attribute<RelativePath> Path = new("NexusMods.MnemonicDB.ArchiveFile/Path");

    public static readonly Attribute<Hash> Hash = new("NexusMods.MnemonicDB.ArchiveFile/Hash");



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
