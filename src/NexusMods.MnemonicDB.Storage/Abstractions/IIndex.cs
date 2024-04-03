using System;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.Storage.Abstractions;

public interface IIndex
{
    int Compare(ReadOnlySpan<byte> readOnlySpan, ReadOnlySpan<byte> readOnlySpan1);
    void Delete(IWriteBatch batch, ReadOnlySpan<byte> span);
    void Put(IWriteBatch batch, ReadOnlySpan<byte> span);
}
