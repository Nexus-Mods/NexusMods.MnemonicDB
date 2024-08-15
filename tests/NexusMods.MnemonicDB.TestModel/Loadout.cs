using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel;

public partial class Loadout : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.TestModel.Loadout";
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    public static readonly BackReferenceAttribute<Mod> Mods = new(Mod.Loadout);
    public static readonly BackReferenceAttribute<Collection> Collections = new(Collection.Loadout);
    public static readonly AbsolutePathAttribute GamePath = new(Namespace, nameof(GamePath)) { IsOptional = true };
}
