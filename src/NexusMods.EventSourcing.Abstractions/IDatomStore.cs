namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// The backing data store for adding and querying datoms
/// </summary>
public interface IDatomStore
{
    /// <summary>
    /// Adds new datoms to the store
    /// </summary>
    /// <param name="source"></param>
    public void Transact(params (ulong E, ulong A, object V, ulong Tx)[] source);
}
