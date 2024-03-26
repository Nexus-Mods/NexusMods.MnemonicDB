using System;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IWriteBatch : IDisposable
{
    public void Commit();

    public void Add(IIndexStore store, ReadOnlySpan<byte> key);
    public void Delete(IIndexStore store, ReadOnlySpan<byte> key);

}
