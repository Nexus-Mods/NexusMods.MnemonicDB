namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// An accumulator is used to accumulate values from events.
/// </summary>
public interface IAccumulator
{
    /// <summary>
    /// Adds a value to the accumulator.
    /// </summary>
    /// <param name="value"></param>
    public void Add(object value);

    /// <summary>
    /// Retracts a value from the accumulator.
    /// </summary>
    /// <param name="value"></param>
    public void Retract(object value);

    /// <summary>
    /// Gets the accumulated value.
    /// </summary>
    /// <returns></returns>
    public object Get();
}
