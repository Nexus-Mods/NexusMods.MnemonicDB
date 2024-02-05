namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Defines a
/// </summary>
public interface IResultSetSink
{
    /// <summary>
    /// Processes the result set
    /// </summary>
    /// <param name="resultSet"></param>
    /// <typeparam name="T"></typeparam>
    public void Process<T>(ref T resultSet) where T : IResultSet;
}
