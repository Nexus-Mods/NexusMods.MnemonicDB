namespace NexusMods.EventSourcing.Abstractions.Nodes.Index;

/// <summary>
/// Appendable index node, allows for merging of data into the node.
/// </summary>
public interface IAppendable : IReadable
{
    /// <summary>
    /// Merges the given data into this node, and possibly returns a new node.
    /// </summary>
    public IAppendable Ingest(Data.IReadable data);
}
