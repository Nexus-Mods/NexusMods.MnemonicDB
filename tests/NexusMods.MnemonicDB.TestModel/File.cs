using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel;

public static class File
{
    public const string Namespace = "NexusMods.MnemonicDB.TestModel.File";

    public static readonly RelativePathAttribute Path = new(Namespace, "Path") {IsIndexed = true};
    public static readonly HashAttribute Hash = new(Namespace, "Hash") {IsIndexed = true};
    public static readonly SizeAttribute Size = new(Namespace, "Size");
    public static readonly ReferenceAttribute ModId = new(Namespace, "Mod");

    public class Model(ITransaction tx, byte partition = (byte)Ids.Partition.Entity)
        : Entity(tx, partition)
    {
        public Model(ITransaction tx) : this(tx, (byte)Ids.Partition.Entity) { }

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
