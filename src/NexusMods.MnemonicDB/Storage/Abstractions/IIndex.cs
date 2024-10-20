using System;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.Storage.Abstractions;

/// <summary>
/// A index definition for a backing datom storage
/// </summary>
public interface IIndex
{
    /// <summary>
    /// Add the delete to the batch
    /// </summary>
    void Delete(IWriteBatch batch, in Datom datom);

    /// <summary>
    /// Add a put to the batch
    /// </summary>
    void Put(IWriteBatch batch, in Datom datom);
}
