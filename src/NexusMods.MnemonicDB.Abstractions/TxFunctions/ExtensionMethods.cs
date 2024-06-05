using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions.TxFunctions;

/// <summary>
/// Extension methods for TxFunctions.
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Adds a function to the transaction as a TxFunction
    /// </summary>
    public static void Add<T>(this ITransaction tx, T arg, Action<ITransaction, IDb, T> fn) =>
        tx.Add(new TxFunction<T>(fn, arg));

    /// <summary>
    /// Adds a function to the transaction as a TxFunction
    /// </summary>
    public static void Add<TA, TB>(this ITransaction tx, TA a, TB b, Action<ITransaction, IDb, TA, TB> fn) =>
        tx.Add(new TxFunction<TA, TB>(fn, a, b));

    /// <summary>
    /// Adds a function to the transaction that will delete the entity with the given id. If `recursive` is true, it will
    /// also delete any entities that reference the given entity.
    /// </summary>
    /// <param name="tx"></param>
    /// <param name="id"></param>
    /// <param name="recursive"></param>
    public static void Delete(this ITransaction tx, EntityId id, bool recursive)
    {
        if (recursive)
        {
            tx.Add(id, DeleteRecursive);
        }
        else
        {
            tx.Add(id, DeleteThisOnly);
        }
    }

    private static void DeleteRecursive(ITransaction tx, IDb db, EntityId eid)
    {
        HashSet<EntityId> seen = [];
        Stack<EntityId> remain = new();
        remain.Push(eid);

        while (remain.Count > 0)
        {
            var current = remain.Pop();
            seen.Add(current);
            DeleteThisOnly(tx, db, current);

            var references = db.ReferencesTo(current);

            foreach (var reference in references)
            {
                if (!seen.Add(reference.E))
                    continue;

                var resolved = reference.Resolved;
                // If recursive, add it to the list of entities to delete
                if (ShouldRecursiveDelete(db, resolved))
                {
                    remain.Push(reference.E);
                }
                else
                {
                    // Otherwise, just delete the reference
                    resolved.Retract(tx);
                }
            }
        }
    }

    /// <summary>
    /// Decide if the entity that contains the given datom should be deleted recursively if this
    /// datom needs to be removed.
    /// </summary>
    private static bool ShouldRecursiveDelete(IDb db, IReadDatom referenceDatom)
    {
        // If the reference is not a collection, then we can delete the whole thing as it's a child of this entity
        if (referenceDatom.A.Cardinalty != Cardinality.Many)
            return true;

        // We can get more detailed, but for now we assume that if the reference is a collection, we should not delete it
        // because it's likely a root of some other system.
        return false;
    }

    private static void DeleteThisOnly(ITransaction tx, IDb db, EntityId eid)
    {
        var segment = db.Get(eid);
        foreach (var datom in segment)
        {
            var resolved = datom.Resolved;
            resolved.Retract(tx);
        }
    }
}
