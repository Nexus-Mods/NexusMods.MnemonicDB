using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.MnemonicDB.ManagedTree.Abstractions;

namespace NexusMods.MnemonicDB.ManagedTree;

public class ManagedTree<TCompare> : IManagedTree
where TCompare : ISpanComparer
{
    private byte[][] _data = [];
    private readonly Comparer<byte[]> _comparer;

    public ManagedTree()
    {
        _comparer = Comparer<byte[]>.Create((x, y) => TCompare.Compare(x, y));
    }

    public IWriteBatch CreateWriteBatch()
    {
        return new WriteBatch<TCompare>(this);
    }
    
    public ISnapshot Commit(WriteBatch<TCompare> writeBatch)
    {
        var toRemove = writeBatch.Ops
            .Where(t => !t.Item1)
            .Select(t => t.Item2)
            .ToImmutableSortedSet(Comparer<byte[]>.Create((x, y) => TCompare.Compare(x, y)));
        
        var toAdd = writeBatch.Ops
            .Where(t => t.Item1)
            .Select(t => t.Item2)
            .ToImmutableSortedSet(Comparer<byte[]>.Create((x, y) => TCompare.Compare(x, y)));
        
        _data = _data.Where(d => !toRemove.Contains(d))
            .Concat(toAdd)
            .OrderBy(d => d, Comparer<byte[]>.Create((x, y) => TCompare.Compare(x, y)))
            .ToArray();

        return new Snapshot<TCompare>(_data);

    }
}

