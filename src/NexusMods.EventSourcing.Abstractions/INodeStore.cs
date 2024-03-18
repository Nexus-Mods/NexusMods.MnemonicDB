

using NexusMods.EventSourcing.Abstractions.Nodes.Data;

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
    /// Puts the node into the store, and returns the assigned key.
    /// </summary>
    public StoreKey Put(IReadable node);


    /// <summary>
    /// Gets the node with the given key, or null if it does not exist.
    /// </summary>
    public IReadable Get(StoreKey key);

}
