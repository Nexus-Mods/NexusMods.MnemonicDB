using System;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;

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
    public void Add(Datom datom);
    
    /// <summary>
    /// Add a delete operation to the batch
    /// </summary>
    public void Delete(Datom datom);
}
