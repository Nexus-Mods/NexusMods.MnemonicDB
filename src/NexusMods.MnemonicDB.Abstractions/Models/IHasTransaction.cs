namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Defines something that is attached to a transaction and has a Transaction reference.
/// </summary>
public interface IHasTransaction
{
    /// <summary>
    /// The transaction reference.
    /// </summary>
    public ITransaction Transaction { get; }
}
