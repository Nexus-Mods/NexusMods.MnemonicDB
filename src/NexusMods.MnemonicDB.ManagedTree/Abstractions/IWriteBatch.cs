using System;

namespace NexusMods.MnemonicDB.ManagedTree.Abstractions;

public interface IWriteBatch
{
    /// <summary>
    /// Add the given data to the write batch as an add
    /// </summary>
    public void Add(ReadOnlySpan<byte> data);

    /// <summary>
    /// Add the given data to the write batch as a delete operation
    /// </summary>
    public void Delete(ReadOnlySpan<byte> data);

    /// <summary>
    /// Commit the write batch to the store and return the resulting snapshot
    /// </summary>
    public ISnapshot Commit();
}
