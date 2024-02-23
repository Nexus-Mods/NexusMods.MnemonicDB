using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public interface INode
{
    public INode Insert<TInput, TDatomComparator>(in TInput inputDatom, in TDatomComparator comparator)
    where TDatomComparator : IDatomComparator
    where TInput : IRawDatom;


    /// <summary>
    /// Ingests the datoms from the `other` iterator into this node and its children.
    /// </summary>
    public INode Ingest<TIterator, TDatom, TDatomStop, TComparator>(in TIterator other,
        in TDatomStop stopDatom, TComparator comparator)
        where TIterator : IIterator<TDatom>
        where TDatom : IRawDatom
        where TDatomStop : IRawDatom
        where TComparator : IDatomComparator;

    /// <summary>
    /// The total number of datoms in this node and all its children.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// The number of child-nodes this node has, or the number of datoms in the node if it is a leaf.
    /// </summary>
    public int ChildCount { get; }

    public IRawDatom LastDatom { get; }

    /// <summary>
    /// Splits this node into two nodes and returns them.
    /// </summary>
    /// <returns></returns>
    public (INode, INode) Split();

    /// <summary>
    /// Returns the size state of this node, if it needs to be split or merged it
    /// will return the appropriate state.
    /// </summary>
    public SizeStates SizeState { get; }

    public IRawDatom this[int idx] { get; }

    public INode Flush(NodeStore store);
}
