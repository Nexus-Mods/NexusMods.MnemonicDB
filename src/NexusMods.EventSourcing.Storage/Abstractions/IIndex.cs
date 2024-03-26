using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IIndex {
    int Compare(ReadOnlySpan<byte> readOnlySpan, ReadOnlySpan<byte> readOnlySpan1);
    void Assert(IWriteBatch store, ReadOnlySpan<byte> datom);
    void Retract(IWriteBatch store, ReadOnlySpan<byte> datom, ReadOnlySpan<byte> previousDatom);
    IDatomIterator GetIterator();
}

