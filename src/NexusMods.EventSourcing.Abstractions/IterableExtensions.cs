using NexusMods.EventSourcing.Abstractions.Iterators;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Helpers for creating iterables from various sources
/// </summary>
public static class IterableExtensions
{
    /// <summary>
    /// Creates an iterator over the elements in this array.
    /// </summary>
    public static ArrayIterator<T> Iterate<T>(this T[] array) => new(array, 0);

}
