using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions.Nodes.Data;
using NexusMods.EventSourcing.Storage;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Interface for a comparator. This is the backbone of the indexes construction
/// code in the storage layer. It is used to sort datoms in arrays, and to distribute
/// them between the B+ Tree nodes. Also, it is used for the sorted merge routines
/// used all throughout the code.
/// </summary>
public interface IDatomComparator
{
    /// <summary>
    /// Get the enum value of the sort order.
    /// </summary>
    public SortOrders SortOrder { get; }

    /// <summary>
    /// Get the attribute registry, associated with the comparator.
    /// </summary>
    public IAttributeRegistry AttributeRegistry { get; }

    /// <summary>
    /// Compares two datoms and returns a value indicating whether one is less than, equal to, or greater than the other.
    /// </summary>
    public int Compare(in Datom x, in Datom y);

    /// <summary>
    /// Make a comparer for the given reader, the comparer will get ints to compare which should be treated as indexes
    /// into the given reader.
    /// </summary>
    public IComparer<int> MakeComparer(IReadable datoms);
}
