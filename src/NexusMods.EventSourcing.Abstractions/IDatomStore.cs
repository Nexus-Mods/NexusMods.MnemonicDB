using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A store for datoms, supports inserting and querying datoms.
/// </summary>
public interface IDatomStore
{
    /// <summary>
    /// Adds a set of new datoms to the store.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <returns></returns>
    void Transact(params (ulong E, ulong A, object V, ulong Tx)[] source);

}

