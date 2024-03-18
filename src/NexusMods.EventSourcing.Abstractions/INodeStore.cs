using NexusMods.EventSourcing.Abstractions.Nodes;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Stores and retrieves nodes, and does serialization and deserialization.
/// </summary>
public interface INodeStore
{
    /// <summary>
    /// Write a new tx to the store, and return the assigned key.
    /// </summary>
    public StoreKey LogTx(INode node);

    /// <summary>
    /// Puts the node into the store, and returns the assigned key.
    /// </summary>
    public StoreKey Put(INode node);


    /// <summary>
    /// Gets the node with the given key, or null if it does not exist.
    /// </summary>
    public INode Get(StoreKey key);

}
