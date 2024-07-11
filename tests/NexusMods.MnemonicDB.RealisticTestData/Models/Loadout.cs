using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.RealisticTestData.Models;

public partial class Loadout : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.RealisticTestData.Models.Loadout";
    
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));

    public static readonly BackReferenceAttribute<Mod> Mods = new(Mod.Loadout);
    
    public static readonly BackReferenceAttribute<ExtractedFile> ExtractedFiles = new(ExtractedFile.Loadout);
}
