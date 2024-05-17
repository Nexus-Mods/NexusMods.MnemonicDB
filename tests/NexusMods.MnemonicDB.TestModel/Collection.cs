using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.TestModel;

public partial class Collection : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.TestModel.Collection";
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
    public static readonly ReferencesAttribute<Mod> Mod = new(Namespace, nameof(Mod));
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout));
}
