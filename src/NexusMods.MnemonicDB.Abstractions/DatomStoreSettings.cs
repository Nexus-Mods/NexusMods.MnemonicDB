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
    public AbsolutePath Path { get; init; }
}
