using System;
using NexusMods.MnemonicDB.ManagedTree.Abstractions;

namespace NexusMods.MnemonicDB.ManagedTree;

public class Iterator<TCompare> : IIterator
where TCompare : ISpanComparer
{
    private readonly byte[][] _data;
    private int _idx;

    public Iterator(byte[][] data)
    {
        _data = data;
        _idx = 0;
    }
    
    public ReadOnlySpan<byte> Current => _data[_idx];
    public void Start()
    {
        _idx = -1;
    }

    public void End()
    {
        throw new NotImplementedException();
    }

    public bool MoveNext()
    {
        var newIdx = _idx + 1;
        if (newIdx < 0 || newIdx >= _data.Length) 
            return false;
        _idx = newIdx;
        return true;
    }

    public bool MovePrev()
    {
        throw new NotImplementedException();
    }

    public bool Seek(ReadOnlySpan<byte> key)
    {
        throw new NotImplementedException();
    }
}
