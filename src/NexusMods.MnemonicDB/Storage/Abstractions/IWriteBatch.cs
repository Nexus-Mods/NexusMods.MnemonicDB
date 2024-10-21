using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.Storage.Abstractions;

/// <summary>
/// A write batch for writing multiple operations to the storage
/// </summary>
public interface IWriteBatch : IDisposable
{
    /// <summary>
    /// Commit the batch to the storage
    /// </summary>
    public void Commit();
    
    /// <summary>
    /// Add a datom to the batch
    /// </summary>
    public void Add(IndexType store, in Datom datom);
    
    /// <summary>
    /// Add a delete operation to the batch
    /// </summary>
    public void Delete(IndexType store, in Datom datom);
}
