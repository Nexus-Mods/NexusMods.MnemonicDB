using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public abstract class AIndexNode : ADataNode, IIndexNode
{
    public abstract IEnumerable<IDataNode> Children { get; }
    public abstract IColumn<int> ChildCounts { get; }

    public abstract IColumn<int> ChildOffsets { get; }
    public abstract IDatomComparator Comparator { get; }

    public abstract IDataNode ChildAt(int idx);


    public override int FindEATV(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        var childIdx = base.FindEATV(0, EntityIds.Length, target, registry);

        var index = ChildAt(childIdx).FindEATV(0, ChildCounts[childIdx], target, registry);

        return index + ChildOffsets[childIdx];
    }

    public override int FindAETV(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        var childIdx = base.FindAETV(0, EntityIds.Length, target, registry);

        var index = ChildAt(childIdx).FindAETV(0, ChildCounts[childIdx], target, registry);

        return index + ChildOffsets[childIdx];
    }

    public override int FindAVTE(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        var childIdx = base.FindAVTE(0, EntityIds.Length, target, registry);

        var index = ChildAt(childIdx).FindAVTE(0, ChildCounts[childIdx], target, registry);

        return index + ChildOffsets[childIdx];
    }

}
