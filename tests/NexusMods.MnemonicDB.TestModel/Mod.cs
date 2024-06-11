﻿using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel;

public partial class Mod : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.TestModel.Mod";
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    public static readonly UriAttribute Source = new(Namespace, nameof(Source)) { IsIndexed = true };
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout));
    public static readonly BackReferenceAttribute<File> Files = new(File.Mod);
    public static readonly MarkerAttribute Marked = new(Namespace, nameof(Marked)) { IsIndexed = true };
    public static readonly StringAttribute Description = new(Namespace, nameof(Description)) { IsOptional = true };
    public static readonly HashAttribute OptionalHash = new(Namespace, nameof(OptionalHash)) { IsOptional = true };
    public static readonly ReferenceAttribute<Mod> LoadAfter = new(Namespace, nameof(LoadAfter)) { IsOptional = true };
}
