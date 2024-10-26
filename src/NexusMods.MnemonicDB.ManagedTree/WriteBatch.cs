using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.ManagedTree.Abstractions;

namespace NexusMods.MnemonicDB.ManagedTree;

public class WriteBatch<TComparer>(ManagedTree<TComparer> tree) : IWriteBatch 
    where TComparer : ISpanComparer
{
    internal readonly List<(bool, byte[])> Ops = new();
    public void Add(ReadOnlySpan<byte> data)
    {
        Ops.Add((true, data.ToArray()));
    }

    public void Delete(ReadOnlySpan<byte> data)
    {
        Ops.Add((false, data.ToArray()));
    }

    public ISnapshot Commit()
    {
        return tree.Commit(this);
    }
}
