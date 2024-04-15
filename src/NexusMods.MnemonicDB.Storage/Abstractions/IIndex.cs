using System;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.Storage.Abstractions;

public interface IIndex
{
    void Delete(IWriteBatch batch, ReadOnlySpan<byte> span);
    void Put(IWriteBatch batch, ReadOnlySpan<byte> span);
}
