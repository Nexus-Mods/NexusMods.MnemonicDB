using System;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;

namespace NexusMods.MneumonicDB.Storage.Abstractions;

public interface IIndex
{
    int Compare(ReadOnlySpan<byte> readOnlySpan, ReadOnlySpan<byte> readOnlySpan1);
    IDatomSource GetIterator();
    void Delete(IWriteBatch batch, ReadOnlySpan<byte> span);
    void Put(IWriteBatch batch, ReadOnlySpan<byte> span);
}
