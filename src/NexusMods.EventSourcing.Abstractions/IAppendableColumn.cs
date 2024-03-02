namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A column that can have data appended to it.
/// </summary>
public interface IAppendableColumn<in T>
{
    /// <summary>
    /// Appends a value to the column.
    /// </summary>
    /// <param name="value"></param>
    public void Append(T value);
}
