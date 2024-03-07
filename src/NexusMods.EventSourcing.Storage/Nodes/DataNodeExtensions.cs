using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public static class DataNodeExtensions
{
    public static IEnumerable<Datom> Range<TChunkA>(this TChunkA chunk, int start, int end)
    where TChunkA : IDataNode
    {
        for (var i = start; i < end; i++)
        {
            yield return chunk[i];
        }
    }

    /// <summary>
    /// Performs a pre-sorted merge of two IEnumerable of Datoms. In the case of a tie, b is preferred.
    /// </summary>
    public static IEnumerable<Datom> Merge<TComparator>(this IEnumerable<Datom> a, IEnumerable<Datom> b, TComparator comparator)
    where TComparator : IDatomComparator
    {
        using var aEnum = a.GetEnumerator();
        using var bEnum = b.GetEnumerator();

        var aHasNext = aEnum.MoveNext();
        var bHasNext = bEnum.MoveNext();

        while (aHasNext && bHasNext)
        {
            var cmp = comparator.Compare(aEnum.Current, bEnum.Current);
            if (cmp < 0)
            {
                yield return aEnum.Current;
                aHasNext = aEnum.MoveNext();
            }
            else
            {
                yield return bEnum.Current;
                bHasNext = bEnum.MoveNext();
            }
        }

        while (aHasNext)
        {
            yield return aEnum.Current;
            aHasNext = aEnum.MoveNext();
        }

        while (bHasNext)
        {
            yield return bEnum.Current;
            bHasNext = bEnum.MoveNext();
        }
    }

}
