using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.MnemonicDB.ManagedTree.Abstractions;

namespace NexusMods.MnemonicDB.ManagedTree;

public class ManagedTree<TCompare> : IDisposable
where TCompare : ISpanComparer
{
    private WritableBlock _data;
    private readonly Comparer<byte[]> _comparer;
    private int[] _sortBuffer = [];
    private WritableBlock _sortBlock;
    private readonly WritableBlockSorter _writableBlockComparator;

    public ManagedTree()
    {
        _data = new WritableBlock();
        _sortBlock = new WritableBlock();
        _comparer = Comparer<byte[]>.Create((x, y) => TCompare.Compare(x, y));
        _writableBlockComparator = new WritableBlockSorter(this);
    }
    
    public ISnapshot Commit(WritableBlock dataToWrite)
    {
        SortBlock(dataToWrite);
        
        
        var newBlock = new WritableBlock();
        MergeOperations(ref newBlock);
        _data = newBlock;
        
        return new Snapshot<TCompare>(newBlock);
    }

    private void SortBlock(WritableBlock dataToWrite)
    {
        // Set the initial indexes
        _sortBuffer = GC.AllocateUninitializedArray<int>(dataToWrite.RowCount);
        for (var i = 0; i < dataToWrite.RowCount; i++)
        {
            _sortBuffer[i] = i;
        }

        _sortBlock = dataToWrite;
        Array.Sort(_sortBuffer, _writableBlockComparator);
    }

    private void MergeOperations(ref WritableBlock newBlock)
    {
        var srcIdx = 0;
        var opIdx = 0;

        while (srcIdx < _data.RowCount && opIdx < _sortBlock.RowCount)
        {
            var srcSpan = _data[srcIdx];
            var opSpan = _sortBlock[_sortBuffer[opIdx]];

            var isWrite = opSpan[0] != 0;
            switch (TCompare.Compare(srcSpan, opSpan.Slice(1)))
            {
                case 0 when !isWrite: 
                    // Delete it by not writing it to the output
                    srcIdx++;
                    opIdx++;
                    break;
                case 0 when isWrite:
                    // Copy over the value if it already exists
                    newBlock.Write(srcSpan);
                    newBlock.NextRow();
                    srcIdx++;
                    opIdx++;
                    break;
                case < 0:
                    // src is smaller, so copy it over
                    newBlock.Write(srcSpan);
                    newBlock.NextRow();
                    srcIdx++;
                    break;
                case > 0 when isWrite:
                    // src is larger
                    newBlock.Write(opSpan.Slice(1));
                    newBlock.NextRow();
                    opIdx++;
                    break;
                case > 0 when !isWrite:
                    // Value is already deleted
                    opIdx++;
                    break;
                default:
                    throw new NotImplementedException();
                    
            }
        }

        // Extra ops to process, so spool them in
        if (opIdx < _sortBlock.RowCount)
        {
            while (opIdx < _sortBlock.RowCount)
            {
                var span = _sortBlock[_sortBuffer[opIdx]];
                // Nothing to delete at this point
                if (span[0] == 0)
                    continue;
                // Slice off the operator
                newBlock.Write(span.Slice(1));
                newBlock.NextRow();
                opIdx++;
            }
        }

        if (srcIdx < _data.RowCount)
        {
            while (srcIdx < _data.RowCount)
            {
                var span = _data[srcIdx];
                newBlock.Write(span);
                newBlock.NextRow();
                srcIdx++;
            }
        }
    }

    private class WritableBlockSorter(ManagedTree<TCompare> tree) : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            var spanX = tree._sortBlock[x];
            var spanY = tree._sortBlock[y];

            var cmp =  TCompare.Compare(spanX.Slice(1), spanY.Slice(1));
            if (cmp == 0)
                return spanX[0].CompareTo(spanY[0]);

            return cmp;
        }
    }
    

    public void Dispose()
    {
    }
}

