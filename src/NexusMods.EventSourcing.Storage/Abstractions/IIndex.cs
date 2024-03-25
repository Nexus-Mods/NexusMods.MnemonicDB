using System;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IIndex<TIndexStore> where TIndexStore : IIndexStore
{
    void Assert<TWriteBatch>(TWriteBatch store, ReadOnlySpan<byte> datom) where TWriteBatch : IWriteBatch<TIndexStore>;
    void Retract<TWriteBatch>(TWriteBatch store, ReadOnlySpan<byte> datom, ReadOnlySpan<byte> previousDatom)
        where TWriteBatch : IWriteBatch<TIndexStore>;
}
