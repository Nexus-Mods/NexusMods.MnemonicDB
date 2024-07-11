using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.RealisticTestData.Models;

public partial class Mod : IModelDefinition
{
    public const string Namespace = "NexusMods.MnemonicDB.RealisticTestData.Models.Mod";
    
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
    
    public static readonly UIntAttribute Priority = new(Namespace, nameof(Priority));
    
    public static readonly MarkerAttribute Enabled = new(Namespace, nameof(Enabled));
    
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout));
}
