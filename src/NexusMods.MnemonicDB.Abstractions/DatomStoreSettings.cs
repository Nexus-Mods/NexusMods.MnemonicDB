using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Settings for the datom store
/// </summary>
public class DatomStoreSettings
{
    public AbsolutePath Path { get; init; }
}
