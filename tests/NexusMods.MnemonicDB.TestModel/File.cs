using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel;

public static class File
{
    public static readonly Attribute<RelativePath> Path = new("NexusMods.MnemonicDB.TestModel.File/Path", isIndexed: true);
    public static readonly Attribute<Hash> Hash = new("NexusMods.MnemonicDB.TestModel.File/Hash", isIndexed: true);
    public static readonly Attribute<Size> Size = new("NexusMods.MnemonicDB.TestModel.File/Size");
    public static readonly Attribute<EntityId> ModId = new("NexusMods.MnemonicDB.TestModel.File/Mod");

    public class Model(ITransaction tx) : AEntity(tx)
    {
        /// <summary>
        /// The path of the file
        /// </summary>
        public RelativePath Path
        {
            get => File.Path.Get(this);
            init => File.Path.Add(this, value);
        }

        /// <summary>
        /// The xxHash64 hash of the file
        /// </summary>
        public Hash Hash
        {
            get => File.Hash.Get(this);
            init => File.Hash.Add(this, value);
        }

        /// <summary>
        /// The size of the file
        /// </summary>
        public Size Size
        {
            get => File.Size.Get(this);
            init => File.Size.Add(this, value);
        }

        /// <summary>
        /// The id of the mod this file belongs to
        /// </summary>
        public EntityId ModId
        {
            get => File.ModId.Get(this);
            init => File.ModId.Add(this, value);
        }

        /// <summary>
        /// The mod this file belongs to
        /// </summary>
        public Mod.Model Mod
        {
            get => Db.Get<Mod.Model>(ModId);
            init => File.ModId.Add(this, value.Id);
        }
    }
}
