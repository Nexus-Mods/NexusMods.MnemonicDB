using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Storage.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.DatomResults;

public static class ExtensionMethods
{
    /// <summary>
    /// Splits the node into sub nodes of the given maximum size, attempts to split the nodes into
    /// blocks roughly of the same size.
    /// </summary>
    public static IEnumerable<IDatomResult> Split(this IDatomResult src, int blockSize)
    {
        var length = src.Length;
        var numBlocks = (length + blockSize - 1) / blockSize;
        var baseBlockSize = length / numBlocks;
        var remainder = length % numBlocks;

        long offset = 0;
        for (var i = 0; i < numBlocks; i++)
        {
            var currentBlockSize = baseBlockSize;
            if (remainder > 0)
            {
                currentBlockSize++;
                remainder--;
            }


            yield return DatomResultView.Create(src, offset, currentBlockSize);
            offset += currentBlockSize;
        }
    }


    internal static string DatomResultToString(this IDatomResult result)
    {
        var className = result.GetType().Name;

        var repr = result.Length switch
        {
            0 => "[]",
            1 => $"[{result[0]}]",
            > int.MaxValue => $"[{result[0]}...]",
            _ => $"[{result[0]} -> {result[(int)(result.Length - 1L)]}]"
        };

        return $"{className}({result.Length}) {repr}";
    }

    private static int[] GetSortIndices(this IDatomResult readable, IDatomComparator comparator)
    {
        var pidxs = GC.AllocateUninitializedArray<int>((int)readable.Length);

        // TODO: may not matter, but we could probably use a vectorized version of this
        for (var i = 0; i < pidxs.Length; i++)
        {
            pidxs[i] = i;
        }

        var comp = comparator.MakeComparer(readable);
        Array.Sort(pidxs, 0, (int)readable.Length, comp);

        return pidxs;
    }

    public static IDatomResult AsSorted(this IDatomResult src, IDatomComparator comparator)
    {
        var indexes = src.GetSortIndices(comparator);
        return new SortedResultView(src, indexes);
    }

    public static DataNode ToDataNode(this IDatomResult src)
    {
        return DataNode.Create(src);
    }

}
