namespace NexusMods.MnemonicDB.ManagedTree.Abstractions;

/// <summary>
/// A snapshot (read-only transaction) of a KVStore. 
/// </summary>
public interface IKVSnapshot
{
    /// <summary>
    /// Get a block by its id.
    /// </summary>
    IMemoryBlock Get(BlockId blockId);
}
