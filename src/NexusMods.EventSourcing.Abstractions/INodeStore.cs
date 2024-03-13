using NexusMods.EventSourcing.Abstractions.Columns.BlobColumns;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Stores and retrieves nodes, and does serialization and deserialization.
/// </summary>
public interface INodeStore
{
    /// <summary>
    /// Write a new tx to the store, and return the assigned key.
    /// </summary>
    public StoreKey LogTx(IReadable node);

    /// <summary>
    /// Flushes the node to the store, and returns a reference node.
    /// </summary>
    public IReadable Flush(IReadable node);
}
