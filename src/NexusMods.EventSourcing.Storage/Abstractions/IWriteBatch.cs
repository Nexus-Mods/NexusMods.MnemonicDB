using System;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IWriteBatch<in TIndexStore> where TIndexStore : IIndexStore
{
    public void Add(TIndexStore store, ReadOnlySpan<byte> key);
    public void Delete(TIndexStore store, ReadOnlySpan<byte> key);
    public void Commit();
}
