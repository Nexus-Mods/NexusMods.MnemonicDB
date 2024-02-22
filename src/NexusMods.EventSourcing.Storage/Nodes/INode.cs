using System.Collections.Generic;

namespace NexusMods.EventSourcing.Storage.Nodes;

public interface INode : IEnumerable<IRawDatom>
{
    public INode Insert<TInput>(TInput inputDatom);

    public INode Remove<TInput>(TInput inputDatom);

    public INode Merge(INode other);

    public (INode, INode) Split();
}
