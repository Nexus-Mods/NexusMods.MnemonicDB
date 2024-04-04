namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

public interface IIndexSegment<TValue>
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
