using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions.Nodes.Index;

/// <summary>
/// Readable index node.
/// </summary>
public interface IReadable : INode
{
    /// <summary>
    /// Get the number of datoms in the child node at the given index, this will include all datoms in the child and its children.
    /// </summary>
    public long GetChildCount(int idx);

    /// <summary>
    /// Gets the datom offset in the child node at the given index. So if the GetChildCount(0) == 100, then
    /// the GetChildOffset(0) == 0 and GetChildOffset(1) == 100.
    /// </summary>
    public long GetChildOffset(int idx);

    /// <summary>
    /// Gets the store key of the child node at the given index.
    /// </summary>
    public StoreKey GetChild(int idx);

    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable ChildCountsColumn { get; }
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable ChildOffsetsColumn { get; }
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable ChildNodeIdsColumn { get; }

    public IDatomComparator Comparator { get; }
}
