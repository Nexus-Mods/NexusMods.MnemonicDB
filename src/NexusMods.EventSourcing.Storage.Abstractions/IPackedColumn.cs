namespace NexusMods.EventSourcing.Storage.Abstractions;

/// <summary>
/// Represents a column of data that is packed into a more efficient format.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IPackedColumn<T> : IColumn<T>
{

}
