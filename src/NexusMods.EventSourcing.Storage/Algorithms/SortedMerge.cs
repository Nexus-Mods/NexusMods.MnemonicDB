using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.Algorithms;

public static class SortedMerge
{
    public static AppendableChunk Merge<TNodeA, TNodeB, TComparator>(TNodeA a, TNodeB b, TComparator comparator)
        where TNodeA : IDataChunk
        where TNodeB : IDataChunk
        where TComparator : IDatomComparator
    {
        var newChunk = new AppendableChunk();

        int i = 0, j = 0;

        while (i < a.Length && j < b.Length)
        {
            var aDatom = a[i];
            var bDatom = b[j];

            var cmp = comparator.Compare(aDatom, bDatom);
            if (cmp < 0)
            {
                newChunk.Append(aDatom);
                i++;
            }
            else if (cmp > 0)
            {
                newChunk.Append(bDatom);
                j++;
            }
            else
            {
                newChunk.Append(aDatom);
                newChunk.Append(bDatom);
                i++;
                j++;
            }
        }

        while (i < a.Length)
        {
            newChunk.Append(a[i]);
            i++;
        }


        while (j < b.Length)
        {
            newChunk.Append(b[j]);
            j++;
        }

        return newChunk;
    }

}
