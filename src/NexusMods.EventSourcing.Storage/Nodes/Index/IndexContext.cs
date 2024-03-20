using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes;
using NexusMods.EventSourcing.Storage.DatomResults;
using NexusMods.EventSourcing.Storage.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Nodes.Index;

public partial class IndexContext
{
    public required int DataNodeSplitThreshold { get; init; }
    public required int IndexNodeSplitThreshold { get; init; }
    public required INodeStore Store { get; init; }
    public required IAttributeRegistry Registry { get; init; }
    public required IDatomComparator Comparator { get; init; }

    public required StoreKey Root { get; set; }

    public void Ingest(IDatomResult additions)
    {
        var root = Store.Get(Root);

        if (root is DataNode dataNode)
        {
            var newDataNode = dataNode.Merge(additions, Comparator);
            if (newDataNode.Length > DataNodeSplitThreshold)
            {
                var split = newDataNode.Split(DataNodeSplitThreshold);
                var list = new List<IndexNode.ChildInfo>();
                foreach (var child in split)
                {
                    var key = Store.Put(child.ToDataNode());
                    var lastDatom = child[(int)(child.Length - 1L)];
                    list.Add(new IndexNode.ChildInfo(key, lastDatom, child.Length));
                }
                Root = Store.Put(IndexNode.Create(list));
                return;
            }
            else
            {
                Root = Store.Put(newDataNode.ToDataNode());
                return;
            }
        }

        if (root is IndexNode indexNode)
        {
            var newIndexNode = (IndexNode)Merge(indexNode, additions, Comparator);
            if (newIndexNode.ShallowLength > IndexNodeSplitThreshold)
            {
                var split = SplitIndex(newIndexNode, IndexNodeSplitThreshold);
                var list = new List<IndexNode.ChildInfo>();
                foreach (var child in split)
                {
                    var key = Store.Put(child);
                    var lastDatom = child.GetLastDatom((int)child.ShallowLength - 1);
                    list.Add(new IndexNode.ChildInfo(key, lastDatom, child.DeepLength));
                }
                Root = Store.Put(IndexNode.Create(list));
                return;
            }
            else
            {
                Root = Store.Put(newIndexNode);
                return;
            }
        }

        throw new NotImplementedException();
    }

    void MaybeSplit(INode node, List<IndexNode.ChildInfo> newChildren)
    {
        if (node is DataNode dataNode)
        {
            if (dataNode.Length >= DataNodeSplitThreshold)
            {
                var split = dataNode.Split(DataNodeSplitThreshold);
                foreach (var child in split)
                {
                    var key = Store.Put(child.ToDataNode());
                    var lastDatom = child[(int)(child.Length - 1L)];
                    newChildren.Add(new IndexNode.ChildInfo(key, lastDatom, child.Length));
                }
            }
            else
            {
                var frozen = dataNode.Freeze();
                newChildren.Add(new IndexNode.ChildInfo(Store.Put(frozen), frozen.All()[(int)(dataNode.Length - 1L)], dataNode.Length));
            }
        }
        else if (node is IndexNode indexChild)
        {
            if (indexChild.DeepLength >= IndexNodeSplitThreshold)
            {
                Debug.Print("Splitting index node.");
                var split = SplitIndex(indexChild, IndexNodeSplitThreshold);
                foreach (var child in split)
                {
                    var key = Store.Put(child);
                    var lastDatom = child.GetLastDatom((int)child.ShallowLength - 1);
                    newChildren.Add(new IndexNode.ChildInfo(key, lastDatom, child.DeepLength));
                }
            }
            else
            {
                newChildren.Add(new IndexNode.ChildInfo(Store.Put(indexChild), indexChild.GetLastDatom((int)indexChild.ShallowLength - 1), indexChild.DeepLength));
            }
        }
        else
        {
            throw new InvalidOperationException("Invalid node type in index node merge operation.");
        }
    }

    private INode Merge(IndexNode indexNode, IDatomResult additions, IDatomComparator comparator)
    {
        long start = 0;
        var newChildren = new List<IndexNode.ChildInfo>();

        for (var i = 0; i < indexNode.ChildKeys.Length; i++)
        {
            var lastDatom = i == indexNode.ChildKeys.Length - 1 ? Datom.Max : indexNode.GetLastDatom(i);
            var last = additions.Find(lastDatom, Comparator);

            if ((last - start) > 0)
            {
                var subView = additions.SubView((int)start, (int)(last - start));
                var newNode = MergeChild(StoreKey.From(indexNode.ChildKeys[i]), subView, comparator);
                MaybeSplit(newNode, newChildren);
                start = last;
            }
            else
            {
                newChildren.Add(new IndexNode.ChildInfo(StoreKey.From(indexNode.ChildKeys[i]), lastDatom, (long)indexNode.ChildCounts[i]));
            }

        }

        return IndexNode.Create(newChildren);
    }

    private INode MergeChild(StoreKey childKey, IDatomResult subView, IDatomComparator comparator)
    {
        var child = Store.Get(childKey);
        if (child is DataNode dataNode)
        {
            var newDataNode = dataNode.Merge(subView, comparator);
            if (newDataNode.Length > DataNodeSplitThreshold)
            {
                var split = newDataNode.Split(DataNodeSplitThreshold);
                var list = new List<IndexNode.ChildInfo>();
                foreach (var splitChild in split)
                {
                    var key = Store.Put(splitChild.ToDataNode());
                    var lastDatom = splitChild[(int)(splitChild.Length - 1L)];
                    list.Add(new IndexNode.ChildInfo(key, lastDatom, splitChild.Length));
                }
                return IndexNode.Create(list);
            }
            else
            {
                return newDataNode.ToDataNode();
            }
        }

        if (child is IndexNode indexChild)
        {
            return Merge(indexChild, subView, comparator);
        }

        throw new NotImplementedException();
    }

    private IEnumerable<IndexNode> SplitIndex(IndexNode node, int blockSize)
    {
        var length = node.ShallowLength;
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


            yield return IndexNode.Create(node, offset, currentBlockSize);
            offset += currentBlockSize;
        }
    }
}
