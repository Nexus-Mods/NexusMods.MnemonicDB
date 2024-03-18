namespace NexusMods.EventSourcing.Abstractions.Nodes.Index;

/// <summary>
/// Appendable index node, allows for merging of data into the node.
/// </summary>
public interface IAppendable : IReadable
{
    /// <summary>
    /// Packs the index node into a packed representation.
    /// </summary>
    /// <returns></returns>
    public IReadable PackIndex(INodeStore store);
}
