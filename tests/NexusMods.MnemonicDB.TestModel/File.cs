using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel;

public interface IFile : IModel
{
    public static class Attributes
    {
        private const string Namespace = "NexusMods.MnemonicDB.TestModel.File";
        public static readonly RelativePathAttribute Path = new(Namespace, "Path") { IsIndexed = true };
        public static readonly HashAttribute Hash = new(Namespace, "Hash") { IsIndexed = true };
        public static readonly SizeAttribute Size = new(Namespace, "Size");
        public static readonly ReferenceAttribute ModId = new(Namespace, "Mod");
    }

    public RelativePath Path { get; set; }

    public Hash Hash { get; set; }

    public Size Size { get; set; }
    public IMod Mod { get; set; }
}
