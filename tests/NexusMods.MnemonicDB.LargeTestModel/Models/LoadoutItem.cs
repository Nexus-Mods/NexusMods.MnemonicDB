using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.LargeTestModel.Attributes;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.LargeTestModel.Models;

/// <summary>
/// A specific item in a loadout
/// </summary>
public partial class LoadoutItem : IModelDefinition
{
    private const string Namespace = "LargeModel.LoadoutItem";

    /// <summary>
    /// The installation location of this item
    /// </summary>
    public static readonly RelativePathAttribute To = new(Namespace, nameof(To));
    
    /// <summary>
    /// The hashcode of this item
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash));
    
    /// <summary>
    /// The size on-disk of this item
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));
    
    /// <summary>
    /// The group this item is part of
    /// </summary>
    public static readonly ReferenceAttribute<Group> Group = new(Namespace, nameof(Group));
    
    /// <summary>
    /// The loadout this item is part of
    /// </summary>
    public static readonly ReferenceAttribute<LargeLoadout> Loadout = new(Namespace, nameof(Loadout));
    
    /// <summary>
    /// If this exists, the file is remapped
    /// </summary>
    public static readonly MarkerAttribute Remapped = new(Namespace, nameof(Remapped));
    
    /// <summary>
    /// If this file is patched, this is the hash it was patched from
    /// </summary>
    public static readonly HashAttribute PatchedFromHash = new(Namespace, nameof(PatchedFromHash)) { IsOptional = true};

    /// <summary>
    /// If this file is patched, this is the ID of the patch
    /// </summary>
    public static readonly GuidAttribute PatchId = new(Namespace, nameof(PatchId)) { IsOptional = true, IsIndexed = true };
    
    /// <summary>
    /// If this is a CreatedBSA file, the tempID of the files inside this archive
    /// </summary>
    public static readonly GuidAttribute TempId = new(Namespace, nameof(TempId)) { IsOptional = true, IsIndexed = true };
}
