using System.Collections.Generic;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A node that is an index, it contains child nodes and also inlines the last node
/// of each child (except the last child that is assumed to be Datom.Max).
/// </summary>
public interface IIndexNode : IDataNode
{
    /// <summary>
    /// The child nodes of this index node, each child's last datom can be accessed
    /// by using the accessors in the IDataNode interface
    /// </summary>
    public IEnumerable<IDataNode> Children { get; }

    /// <summary>
    /// The count of datoms in each child
    /// </summary>
    public IColumn<int> ChildCounts { get; }

    /// <summary>
    /// The offset of each child in the data node, so if the first child has 1255 datoms, the offset of
    /// the second child will be 1255.
    /// </summary>
    public IColumn<int> ChildOffsets { get; }

    /// <summary>
    /// The comparator used to compare datoms in this index node.
    /// </summary>
    public IDatomComparator Comparator { get; }

    /// <summary>
    /// Gets the child at the specified index.
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public IDataNode ChildAt(int idx);
}
