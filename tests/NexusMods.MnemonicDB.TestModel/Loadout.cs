using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.TestModel;

public partial class Loadout : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.TestModel.Loadout";
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
    public static readonly BackReferenceAttribute<Mod> Mods = new(Mod.Loadout);
}
