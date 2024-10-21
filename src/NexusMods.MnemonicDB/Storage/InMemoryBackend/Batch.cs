using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage.Abstractions;
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
    public void Add(IndexType index, in Datom datom)
    {
        var iDatom = new IndexDatom
        {
            Index = index,
            Datom = datom,
        };
        _datoms.Add((false, iDatom.ToArray()));
    }
    
    /// <inheritdoc />
    public void Delete(IndexType index, in Datom datom)
    {
        var iDatom = new IndexDatom
        {
            Index = index,
            Datom = datom,
        };
        _datoms.Add((true, iDatom.ToArray()));
    }
}
