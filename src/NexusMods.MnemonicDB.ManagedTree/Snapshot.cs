using System;
using NexusMods.MnemonicDB.ManagedTree.Abstractions;

namespace NexusMods.MnemonicDB.ManagedTree;


public class Snapshot<T> : ISnapshot
where T : ISpanComparer
{
    private readonly byte[][] _data;

    public Snapshot(byte[][] data)
    {
        _data = data;
    }

    public IIterator GetIterator()
    {
        return new Iterator<T>(_data);
    }
}
