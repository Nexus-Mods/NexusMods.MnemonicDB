using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Nodes.Index;
using NexusMods.EventSourcing.Storage.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Nodes.Index;

public static class ExtensionMethods
{
    internal record struct ChildInfo(Datom LastDatom, StoreKey Child, ulong DeepLength);

    static void MaybeSplit(EventSourcing.Abstractions.Nodes.Data.IReadable src, List<ChildInfo> children, INodeStore store)
    {
        if (src is IAppendable index && index.Length > Configuration.IndexBlockSize)
        {
            var split = index.Split(Configuration.IndexBlockSize).ToArray();
            foreach (var child in split)
            {
                var key = store.Put(child);
                children.Add(new ChildInfo(child.LastDatom, key, (ulong)child.DeepLength));
            }
        }
        else if (src is EventSourcing.Abstractions.Nodes.Data.IReadable data &&
                 data.Length > Configuration.DataBlockSize)
        {
            var split = data.Split(Configuration.DataBlockSize).ToArray();
            foreach (var child in split)
            {
                var key = store.Put(child);
                children.Add(new ChildInfo(child.LastDatom, key, (ulong)child.DeepLength));
            }
        }
        else
        {
            var key = store.Put(src);
            children.Add(new ChildInfo(src.LastDatom, key, (ulong)src.DeepLength));
        }

    }

    /// <summary>
    /// Ingest the given datoms into the index node.
    /// </summary>
    public static IReadable Ingest(this IReadable index, INodeStore store, EventSourcing.Abstractions.Nodes.Data.IReadable node)
    {
        var start = 0;

        var children = new List<ChildInfo>();


        var childCount = index.ChildCountsColumn.Length;

        for (var idx = 0; idx < childCount; idx++)
        {
            var childKey = index.GetChild(idx);
            var child = store.Get(childKey);
            var datom = idx == childCount - 1 ? Datom.Max : child.LastDatom;
            var last = node.Find(datom, index.Comparator);

            if (last < node.Length)
            {
                var newNode = child.Merge(node.SubView(start, last - start), index.Comparator);
                MaybeSplit(newNode, children, store);
                start = last;
            }
            else if (last == node.Length)
            {
                var newNode = child.Merge(node.SubView(start, last - start), index.Comparator);
                MaybeSplit(newNode, children, store);

                for (var i = idx + 1; i < childCount; i++)
                {
                    // We want to be careful here and not load the entire child node into memory, because there's no reason
                    // to load thousands of nodes just so we can update one of them.
                    childKey = index.GetChild(i);
                    var lastDatom = new Datom
                    {
                        E = index.GetEntityId(i),
                        A = index.GetAttributeId(i),
                        V = index.ValuesColumn.GetValue(i),
                        T = index.GetTransactionId(i)
                    };
                    children.Add(new ChildInfo(lastDatom, childKey, (ulong)index.GetChildCount(idx)));
                }

                break;
            }
        }

        return new Appendable(index.Comparator, children);
    }

}
