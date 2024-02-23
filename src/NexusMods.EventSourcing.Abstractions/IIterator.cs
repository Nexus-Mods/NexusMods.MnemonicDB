using System.Diagnostics.CodeAnalysis;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A composable, strictly typed, iterator with support for filtering
/// and stack based iteration.
/// </summary>
public interface IIterator<T>
{
    /// <summary>
    /// Moves to the next item in the iterator, returns false if the
    /// iterator is at the end of the sequence. The return value will
    /// always be the same as the AtEnd property at the end of the call,
    /// but it's returned here reduce the number of calls in the common
    /// use case.
    /// </summary>
    /// <returns></returns>
    public bool Next();

    /// <summary>
    /// Returns true if the iterator is at the end of the sequence.
    /// </summary>
    public bool AtEnd { get; }

    /// <summary>
    /// Gets the current item in the iterator, if the current value
    /// is not valid, the out value will be set to the default value
    /// and the method will return false.
    /// </summary>
    /// <param name="value"></param>
    public bool Value(out T value);

}
