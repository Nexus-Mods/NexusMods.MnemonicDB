using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

namespace NexusMods.EventSourcing.Abstractions.Nodes;

/// <summary>
/// Base interface for all nodes, casts are used to get the specific type.
/// </summary>
public interface INode
{
    /// <summary>
    /// Gets all the datoms in the node as a <see cref="IDatomResult"/>.
    /// </summary>
    public IDatomResult All();
}
