using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Represents a transaction, which is a set of proposed changes to the datom store
/// </summary>
public interface ITransaction
{
    /// <summary>
    ///     Gets the temporary id for the transaction
    /// </summary>
    public TxId ThisTxId { get; }

    /// <summary>
    ///     Gets a temporary id for a new entity
    /// </summary>
    /// <returns></returns>
    EntityId TempId();

    /// <summary>
    ///     Adds a new datom to the transaction
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="val"></param>
    /// <typeparam name="TAttribute"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    void Add<TAttribute, TVal>(EntityId entityId, TVal val)
        where TAttribute : IAttribute<TVal>;

    /// <summary>
    ///     Commits the transaction
    /// </summary>
    Task<ICommitResult> Commit();

    ModelHeader New();
}
