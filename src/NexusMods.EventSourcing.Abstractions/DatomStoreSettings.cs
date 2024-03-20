using NexusMods.Paths;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Settings for the datom store
/// </summary>
public class DatomStoreSettings
{
    public AbsolutePath Path { get; init; }
}
