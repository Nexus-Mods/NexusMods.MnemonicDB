using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public interface INode : IEnumerable<IRawDatom>
{
    public INode Insert<TInput, TDatomComparator>(in TInput inputDatom, in TDatomComparator comparator)
    where TDatomComparator : IDatomComparator
    where TInput : IRawDatom;

    public INode Remove<TInput>(TInput inputDatom);

    public INode Merge(INode other);

    public (INode, INode) Split();
}
