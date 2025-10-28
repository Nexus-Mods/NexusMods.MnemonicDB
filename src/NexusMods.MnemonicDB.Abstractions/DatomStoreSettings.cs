using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Settings for the datom store
/// </summary>
public class DatomStoreSettings
{
    /// <summary>
    /// Settings for a default in-memory datom store
    /// </summary>
    public static DatomStoreSettings InMemory { get; } = new() { Path = null };
    
    /// <summary>
    /// True if the datom store is read only (cannot be written to)
    /// </summary>
    public bool IsReadOnly { get; set; } = false;
    
    /// <summary>
    /// The path to the datom store's storage
    /// </summary>
    public AbsolutePath? Path { get; init; } = null;
    
    /// <summary>
    /// True if the path is null representing a in-memory store
    /// </summary>
    public bool IsInMemory => Path is null;
}
