using System;
using NexusMods.HyperDuck;
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
    /// Add a datom to the batch after it's remapped to the given index
    /// </summary>
    public void Add(IndexType index, Datom datom) => 
        Add(datom with { Prefix = datom.Prefix with { Index = index }});

    /// <summary>
    /// Add a datom to the batch
    /// </summary>
    public void Add(Datom datom);
    
    /// <summary>
    /// Add a datom to the batch
    /// </summary>
    public void Add(ValueDatom datom);
    
    /// <summary>
    /// Add a delete operation to the batch, after remapping the datom to the given index
    /// </summary>
    public void Delete(IndexType index, Datom datom) => 
        Delete(datom with { Prefix = datom.Prefix with { Index = index }});
    
    /// <summary>
    /// Add a delete operation to the batch
    /// </summary>
    public void Delete(Datom datom);
    
    /// <summary>
    /// Add a delete operation to the batch
    /// </summary>
    public void Delete(ValueDatom datom);
}
