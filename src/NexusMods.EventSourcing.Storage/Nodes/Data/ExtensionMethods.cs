using System;
using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes;
using NexusMods.EventSourcing.Storage.Columns.BlobColumns;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;
using NexusMods.EventSourcing.Storage.DatomResults;
using NexusMods.EventSourcing.Storage.Nodes.Index;

namespace NexusMods.EventSourcing.Storage.Nodes.Data;

public static class ExtensionMethods
{
    /// <summary>
    /// Returns the indices that would sort the <see cref="IReadable"/> according to the given <see cref="IDatomComparator"/>.
    /// </summary>
    public static int[] GetSortIndices(this IDatomResult readable, IDatomComparator comparator)
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

    /// <summary>
    /// Creates a new IReadable by creating a read-only view of a portion of the given IReadable.
    /// </summary>
    public static IDatomResult SubView(this IDatomResult src, int offset, int length)
    {
        return new DatomResultView(src, offset, length);
    }


    public static IDatomResult Merge(this IDatomResult src, IDatomResult other, IDatomComparator comparator)
    {
        var newNode = DataNode.Create();
        newNode.Add(src);
        newNode.Add(other);
        newNode.Freeze();
        return newNode.AsSorted(comparator);
    }




    /*




    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int FindEATV(this IReadable readable, in Datom target, IAttributeRegistry registry)
    {
        return readable.FindEATVReader(target, registry);
    }

    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int FindAETV(this IReadable readable, in Datom target, IAttributeRegistry registry)
    {
        return readable.FindAETVReader(target, registry);
    }

    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int FindAVTE(this IReadable readable, in Datom target, IAttributeRegistry registry)
    {
        return readable.FindAVTEReader(target, registry);
    }



    /// <summary>
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static int Find(this IReadable readable, in Datom target, IDatomComparator comparator)
    {
        return readable.Find(target, comparator.SortOrder, comparator.AttributeRegistry);
    }

    /// <summary>
    /// Slower version of <see cref="Pack"/> that requires copying every column into a span, then packing it.
    /// This is required for nodes that are not <see cref="IAppendable"/> and not <see cref="IPacked"/>, such as
    /// views and sorted nodes.
    /// </summary>
    private static IReadable PackSlow(this IReadable readable, INodeStore store)
    {
        return new DataPackedNode
        {
            Length = readable.Length,
            EntityIds = (ULongColumn)readable.EntityIdsColumn.Pack(),
            AttributeIds = (ULongColumn)readable.AttributeIdsColumn.Pack(),
            Values = (BlobColumn)readable.ValuesColumn.Pack(),
            TransactionIds = (ULongColumn)readable.TransactionIdsColumn.Pack()
        };
    }

    public static IReadable ReadDataNode(ReadOnlyMemory<byte> writerWrittenMemory)
    {
        var dataPackedNode = DataPackedNode.Serializer.Parse(writerWrittenMemory);
        return dataPackedNode;
    }

    public static IReadable Merge(this INode src, IReadable other, IDatomComparator comparator)
    {
        switch (src)
        {
            case EventSourcing.Abstractions.Nodes.Index.IReadable index:
                return MergeIndex(index, other, comparator);
            case IReadable readable:
                return MergeData(src, other, comparator);
            default:
                throw new NotImplementedException();
        }
    }

    private static IReadable MergeIndex(INode index, IReadable other, IDatomComparator comparator, INodeStore store)
    {
        if (index is EventSourcing.Abstractions.Nodes.Index.IAppendable appendable)
            return appendable.Ingest(other, store);
        throw new NotImplementedException();
    }

    private static IReadable MergeData(IReadable src, IReadable other, IDatomComparator comparator)
    {
        // TODO: use sorted merge, maybe?
        var appendable = Appendable.Create(src);
        appendable.Add(other);
        return appendable.AsSorted(comparator);
    }


    */
}
