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

    /// <summary>
    /// Gets all datoms for a given entity as of a given time
    /// </summary>
    /// <param name="e"></param>
    /// <param name="sink"></param>
    /// <param name="t"></param>
    /// <typeparam name="TSink"></typeparam>
    public void QueryByE<TSink>(ulong e, ref TSink sink, ulong t) where TSink : IResultSetSink;
}
