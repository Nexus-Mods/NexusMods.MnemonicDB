namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents an iterator over a collection of values.
/// </summary>
/// <typeparam name="TIterator"></typeparam>
/// <typeparam name="TValue"></typeparam>
public interface IIterable<TIterator, TValue>
    where TIterator : IIterator<TValue>
{
    /// <summary>
    /// Returns an iterator over the elements in this object.
    /// </summary>
    /// <returns></returns>
    public TIterator Iterate();
}
