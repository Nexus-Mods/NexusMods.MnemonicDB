namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// An entity that is temporary and is added to a transaction during construction.
/// </summary>
public interface ITemporaryEntity : IHasEntityId
{
    /// <summary>
    /// Used internally by a transaction to add the entity's datoms to the transaction.
    /// </summary>
    /// <param name="tx"></param>
    public void AddTo(ITransaction tx);
}
