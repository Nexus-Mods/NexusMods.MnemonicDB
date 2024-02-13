using System;

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
    /// Commits the transaction
    /// </summary>
    ICommitResult Commit();
}
