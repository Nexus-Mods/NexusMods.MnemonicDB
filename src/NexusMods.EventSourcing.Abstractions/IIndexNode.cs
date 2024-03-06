using System.Collections.Generic;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Abstractions;

public interface IIndexNode : IDataNode
{
    /// <summary>
    /// The child nodes of this index node, each child's last datom can be accessed
    /// by using the accessors in the IDataNode interface
    /// </summary>
    public IEnumerable<IDataNode> Children { get; }

    public IColumn<int> ChildCounts { get; }
    public IColumn<int> ChildOffsets { get; }
    public IDatomComparator Comparator { get; }
    public IDataNode ChildAt(int idx);
}
