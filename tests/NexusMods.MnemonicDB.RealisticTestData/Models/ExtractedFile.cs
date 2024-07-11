using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.RealisticTestData.Models;

public partial class ExtractedFile : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.RealisticTestData.Models.ExtractedFile";
    
    public static readonly HashAttribute ArchiveHash = new(Namespace, nameof(ArchiveHash));
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash));
    public static readonly RelativePathAttribute To = new(Namespace, nameof(To));
    public static readonly RelativePathAttribute ArchivePath = new(Namespace, nameof(ArchivePath));
    public static readonly ReferenceAttribute<Mod> Mod = new(Namespace, nameof(Mod));
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout));
}
