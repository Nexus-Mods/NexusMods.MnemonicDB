using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel;

public interface IMod : IModel
{
    public static class Attributes
    {
        private const string Namespace = "NexusMods.MnemonicDB.TestModel.Mod";

        public static readonly StringAttribute Name = new(Namespace, "Name") { IsIndexed = true };
        public static readonly UriAttribute Source = new(Namespace, "Source");
        public static readonly ReferenceAttribute LoadoutId = new(Namespace, "Loadout");
        public static readonly MarkerAttribute IsMarked = new(Namespace, "IsMarked");
    }

    public string Name { get; set; }

    public Uri Source { get; set; }

    public ILoadout Loadout { get; set; }

    public bool IsMarked { get; set; }

    public IEnumerable<IFile> Files { get; }
}
