namespace NexusMods.MnemonicDB.ManagedTree.Abstractions;

/// <summary>
/// A key-value store for blocks.
/// </summary>
public interface IKVStore
{
    /// <summary>
    /// Create a new snapshot (read-only transaction) of the KVStore.
    /// </summary>
    public IKVSnapshot Snapshot();
    
    /// <summary>
    /// Put a block by its id.
    /// </summary>
    public void Put(BlockId blockId, IMemoryBlock block);
    
    /// <summary>
    /// Delete a block by its id.
    /// </summary>
    /// <param name="blockId"></param>
    public void Delete(BlockId blockId);
}
