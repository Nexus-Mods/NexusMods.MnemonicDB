namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Represents the cardinality of an attribute
/// </summary>
public enum Cardinality
{
    /// <summary>
    /// Only one value per entity
    /// </summary>
    One = 1,

    /// <summary>
    /// Zero or one values per entity
    /// </summary>
    Many = 2
}
