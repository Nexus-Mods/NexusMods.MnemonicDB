using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.Algorithms;

public static class SortedMerge
{
    public static AppendableNode Merge<TNodeA, TNodeB, TComparator>(TNodeA a, TNodeB b, TComparator comparator)
        where TNodeA : IDataNode
        where TNodeB : IDataNode
        where TComparator : IDatomComparator
    {
        var newNode = new AppendableNode();

        int i = 0, j = 0;

        while (i < a.Length && j < b.Length)
        {
            var aDatom = a[i];
            var bDatom = b[j];

            var cmp = comparator.Compare(aDatom, bDatom);
            if (cmp < 0)
            {
                newNode.Append(aDatom);
                i++;
            }
            else if (cmp > 0)
            {
                newNode.Append(bDatom);
                j++;
            }
            else
            {
                newNode.Append(aDatom);
                newNode.Append(bDatom);
                i++;
                j++;
            }
        }

        while (i < a.Length)
        {
            newNode.Append(a[i]);
            i++;
        }


        while (j < b.Length)
        {
            newNode.Append(b[j]);
            j++;
        }

        return newNode;
    }

}
