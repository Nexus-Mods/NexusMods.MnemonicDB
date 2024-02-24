using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;

namespace NexusMods.EventSourcing.Storage.Nodes;

/// <summary>
/// A node that points to some other node in storage,
/// lazily loading the node from storage when needed.
/// </summary>
public class ReferenceNode(NodeStore store) : INode
{
    public required UInt128 Id { get; init; }
    private INode? _node;

    private INode Deref()
    {
        if (_node is null)
        {
            _node = store.Load(Id);
        }
        return _node;
    }

    public INode Insert<TInput, TDatomComparator>(in TInput inputDatom, in TDatomComparator comparator) where TInput : IRawDatom where TDatomComparator : IDatomComparator
    {
        throw new NotImplementedException();
    }

    public INode Ingest<TIterator, TDatom, TDatomStop, TComparator>(in TIterator other, in TDatomStop stopDatom,
        TComparator comparator) where TIterator : IIterator<TDatom> where TDatom : IRawDatom where TDatomStop : IRawDatom where TComparator : IDatomComparator
    {
        return Deref().Ingest<TIterator, TDatom, TDatomStop, TComparator>(other, stopDatom, comparator);
    }

    public required int Count { get; init; }
    public required int ChildCount { get; init; }
    public required IRawDatom LastDatom { get; init; }
    public (INode, INode) Split()
    {
        throw new NotImplementedException();
    }

    public SizeStates SizeState => SizeStates.Ok;

    public IRawDatom this[int idx] => Deref()[idx];
    public INode Flush(NodeStore store)
    {
        return this;
    }
}
