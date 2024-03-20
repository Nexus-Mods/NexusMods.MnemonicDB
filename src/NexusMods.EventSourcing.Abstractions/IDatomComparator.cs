using System.Collections.Generic;

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
    /// Compares two datoms and returns a value indicating whether one is less than, equal to, or greater than the other.
    /// </summary>
    public int Compare(in Datom x, in Datom y);

    /// <summary>
    /// Make a comparer for the given datoms using indices as a key to the datoms. This is used
    /// so that the .NET sorting algorithm can sort be used to sort datoms, but yet we can still
    /// only retrieve the parts of of the datoms that we need. In other words if to datoms differ
    /// only in the E part, we don't need to compare the A, T and V parts (V being a rather expensive
    /// comparison to make). This comparer is unsafe as the MemoryDatom contains raw pointers to
    /// arrays of integers being sorted in memory.
    /// </summary>
    public unsafe IComparer<int> MakeComparer<TBlob>(MemoryDatom<TBlob> datoms)
        where TBlob : IBlobColumn;
}
