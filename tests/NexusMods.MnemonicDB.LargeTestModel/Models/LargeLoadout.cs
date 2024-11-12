using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.LargeTestModel.Models;

/// <summary>
/// Loadout, the top-level model
/// </summary>
public partial class LargeLoadout : IModelDefinition
{
    private const string Namespace = "LargeModel.Loadout";

    public static readonly StringAttribute Name = new(Namespace, "Name") { IsIndexed = true };

}
