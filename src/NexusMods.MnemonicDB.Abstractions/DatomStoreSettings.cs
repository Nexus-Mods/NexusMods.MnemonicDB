using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Settings for the datom store
/// </summary>
public class DatomStoreSettings
{
    /// <summary>
    /// The path to the datom store's storage
    /// </summary>
    public AbsolutePath Path { get; init; } = default;

    /// <summary>
    /// True if the store is in-memory, this is true if the path is not set.
    /// </summary>
    public bool IsInMemory => Path == default;

    /// <summary>
    /// Settings for an in-memory datom store
    /// </summary>
    public static DatomStoreSettings CreateInMemory()
    {
        return new DatomStoreSettings { Path = default };
    }
}
