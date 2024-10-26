using System;
using NexusMods.MnemonicDB.ManagedTree.Abstractions;

namespace NexusMods.MnemonicDB.ManagedTree;


public class Snapshot<T> : ISnapshot
where T : ISpanComparer
{
    private readonly WritableBlock _data;

    public Snapshot(WritableBlock data)
    {
        _data = data;
    }

    public IIterator GetIterator()
    {
        return new Iterator<T>(_data);
    }
}
