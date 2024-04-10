namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Represents the cardinality of an attribute
/// </summary>
public enum Cardinality
{
    /// <summary>
    /// Only one value per entity
    /// </summary>
    One,

    /// <summary>
    /// Zero or one values per entity
    /// </summary>
    Many
}
