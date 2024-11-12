using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.LargeTestModel.Models;

/// <summary>
/// A group of loadout items
/// </summary>
public partial class Group : IModelDefinition
{
    private const string Namespace = "LargeModel.Group";
    
    /// <summary>
    /// The name of the group
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };

    /// <summary>
    /// The loadout this group is part of
    /// </summary>
    public static readonly ReferenceAttribute<LargeLoadout> Loadout = new(Namespace, nameof(Loadout));
    
    /// <summary>
    /// The existance of this marker means the group is disabled
    /// </summary>
    public static readonly MarkerAttribute Disabled = new(Namespace, nameof(Disabled));
}
