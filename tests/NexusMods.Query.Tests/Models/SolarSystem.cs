using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Tests.Models.Attributes;
using NexusMods.Query.Tests.Models.Attributes;

namespace NexusMods.Query.Tests.Models;

public enum SecurityClass : byte
{
    High = 1,
    Low,
    Null
}

/// <summary>
/// Model for test data driven from EVE Online's public star system data
/// </summary>
public partial class SolarSystem : IModelDefinition
{
    private const string Namespace = "SolarSystem";

    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    
    public static readonly FloatAttribute SecurityLevel = new(Namespace, nameof(SecurityLevel)) { IsIndexed = true };
    
    public static readonly StringAttribute SecurityClass = new(Namespace, nameof(SecurityClass)) { IsIndexed = true };

    public static readonly SecurityStatusAttribute SecurityStatus =
        new(Namespace, nameof(SecurityStatus)) { IsIndexed = true };
    
    /// <summary>
    /// Jumps out of this solar system
    /// </summary>
    public static readonly ReferencesAttribute<SolarSystem> JumpsOut = new(Namespace, "JumpsTo");

    /// <summary>
    /// Jumps into this solar system
    /// </summary>
    public static readonly BackReferenceAttribute<SolarSystem> JumpsIn = new(JumpsOut);
}
