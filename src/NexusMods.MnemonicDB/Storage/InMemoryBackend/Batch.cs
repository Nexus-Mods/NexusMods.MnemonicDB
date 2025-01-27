using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using IWriteBatch = NexusMods.MnemonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MnemonicDB.Storage.InMemoryBackend;

internal class Batch(Backend backend) : IWriteBatch
{
    private readonly List<(bool IsDelete, byte[] Data)> _datoms = [];

    /// <inheritdoc />
    public void Dispose() { }

    /// <inheritdoc />
    public void Commit()
    {
        backend.Alter(oldSet =>
        {
            var set = oldSet.ToBuilder();
            foreach (var (isDelete, data) in _datoms)
            {
                if (isDelete)
                    set.Remove(data);
                else
                    set.Add(data);
            }
            return set.ToImmutable();
        });
    }
    
    /// <inheritdoc />
    public void Add(Datom datom)
    {
        _datoms.Add((false, datom.ToArray()));
    }
    
    /// <inheritdoc />
    public void Delete(Datom datom)
    {
        _datoms.Add((true, datom.ToArray()));
    }
}
