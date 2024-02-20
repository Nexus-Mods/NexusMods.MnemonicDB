using System;
using NexusMods.EventSourcing.Abstractions.Models;

namespace NexusMods.EventSourcing.Abstractions;

public interface ITransaction
{
    /// <summary>
    /// Gets a temporary id for a new entity
    /// </summary>
    /// <returns></returns>
    EntityId TempId();

    /// <summary>
    /// Adds a new datom to the transaction
    /// </summary>
    void Add(IDatom datom);


    /// <summary>
    /// Adds a new read model to the transaction, the datoms are extracted from the read model
    /// as asserts for each property with the FromAttribute
    /// </summary>
    /// <param name="model"></param>
    TReadModel Add<TReadModel>(TReadModel model)
        where TReadModel : IReadModel;

    /// <summary>
    /// Adds a new datom to the transaction
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="val"></param>
    /// <param name="isAssert"></param>
    /// <typeparam name="TAttribute"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    void Add<TAttribute, TVal>(EntityId entityId, TVal val, bool isAssert = true)
        where TAttribute : IAttribute<TVal>;

    /// <summary>
    /// Commits the transaction
    /// </summary>
    ICommitResult Commit();

    /// <summary>
    /// Gets the temporary id for the transaction
    /// </summary>
    public TxId ThisTxId { get; }
}
