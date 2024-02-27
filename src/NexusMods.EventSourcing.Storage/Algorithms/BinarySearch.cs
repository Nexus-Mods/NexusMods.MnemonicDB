using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.Algorithms;

public static class BinarySearch
{
    public static int SeekEqualOrLess<TNode, TComparator>(TNode node, TComparator comparator, int start, int end, in Datom target)
        where TNode : IDataChunk
        where TComparator : IDatomComparator
    {
        while (start < end)
        {
            var mid = start + (end - start) / 2;
            var cmp = comparator.Compare(target, node[mid]);
            if (cmp > 0)
            {
                start = mid + 1;
            }
            else
            {
                end = mid;
            }
        }

        return start;
    }
}
