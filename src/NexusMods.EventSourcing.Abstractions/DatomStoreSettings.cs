namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Settings for the datom store
/// </summary>
public class DatomStoreSettings
{
    /// <summary>
    /// The maximum number of datoms to keep in memory before flushing to the underlying storage.
    /// </summary>
    public int MaxInMemoryDatoms { get; set; } = 1024 * 256;
}
