namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// The result of a transaction commit, contains metadata useful for looking up the results of the transaction
/// </summary>
public interface ICommitResult
{
    /// <summary>
    /// Remaps a temporary id to a permanent id, or returns the original id if it was not a temporary id
    /// </summary>
    /// <param name="id"></param>
    public EntityId this[EntityId id] { get; }


    /// <summary>
    /// Gets the new TxId after the commit
    /// </summary>
    public TxId NewTx { get; }
}
