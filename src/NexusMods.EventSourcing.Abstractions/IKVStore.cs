using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Implements a interface for loading and saving blocks of data.
/// </summary>
public interface IKvStore : IDisposable
{
    /// <summary>
    /// Puts the value into the store with the given key.
    /// </summary>
    public void Put(StoreKey key, ReadOnlySpan<byte> value);

    /// <summary>
    /// Tried to get the value from the store with the given key.
    /// </summary>
    public bool TryGet(StoreKey key, out ReadOnlySpan<byte> value);

    /// <summary>
    /// Deletes the value from the store with the given key.
    /// </summary>
    /// <param name="key"></param>
    public void Delete(StoreKey key);

    /// <summary>
    /// If any transaction has been committed, return the latest transaction id.
    /// </summary>
    public bool TryGetLatestTx(out TxId key);
}
