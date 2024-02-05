namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// The backing data store for adding and querying datoms
/// </summary>
public interface IDatomStore
{
    /// <summary>
    /// Adds new datoms to the store
    /// </summary>
    /// <param name="socket"></param>
    /// <typeparam name="TSocket"></typeparam>
    public void Transact<TSocket>(ref TSocket socket) where TSocket : IDatomSinkSocket;
}
