using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.RealisticTestData.Models;

public partial class Archive : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.RealisticTestData.Models.Archive";
    
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash));
    
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));
    
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
    
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout));
}
