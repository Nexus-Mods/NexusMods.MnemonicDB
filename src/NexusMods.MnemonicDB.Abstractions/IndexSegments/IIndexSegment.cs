namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// An index segment that reutrns values of a specific type
/// </summary>
/// <typeparam name="TValue"></typeparam>
public interface IIndexSegment<out TValue>
{
    /// <summary>
    /// Gets the value at the given index
    /// </summary>
    public TValue this[int idx] { get; }

    /// <summary>
    /// Gets the number of items in the collection
    /// </summary>
    public int Count { get; }
}
