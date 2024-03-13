namespace NexusMods.EventSourcing.Abstractions.Nodes.Index;

/// <summary>
/// Readable index node.
/// </summary>
public interface IReadable : Data.IReadable
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
    /// Gets the child node at the given index.
    /// </summary>
    public Data.IReadable GetChild(int idx);
}
