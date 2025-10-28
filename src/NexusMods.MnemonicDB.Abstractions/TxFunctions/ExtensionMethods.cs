using System;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.TxFunctions;

/// <summary>
/// Extension methods for TxFunctions.
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Adds a function to the transaction that will delete the entity with the given id. If `recursive` is true, it will
    /// also delete any entities that reference the given entity.
    /// </summary>
    /// <param name="tx"></param>
    /// <param name="id"></param>
    /// <param name="recursive"></param>
    public static void Delete(this Datoms tx, EntityId id, bool recursive = false)
    {
        if (recursive)
        {
            tx.Add(new DeleteRecursiveFn(id));
        }
        else
        {
            tx.Add(new DeleteThisOnlyFn(id));
        }
    }

    private readonly record struct DeleteRecursiveFn(EntityId Eid) : ITxFunction
    {
        public void Apply(Transaction tx)
        {
            var db = tx.BasisDb;
            HashSet<EntityId> seen = [];
            Stack<EntityId> remain = new();
            remain.Push(Eid);
            var cache = db.AttributeResolver.AttributeCache;

            while (remain.Count > 0)
            {
                var current = remain.Pop();
                seen.Add(current);
                new DeleteThisOnlyFn(Eid).Apply(tx);

                var references = db.ReferencesTo(current);

                foreach (var reference in references)
                {
                    if (!seen.Add(reference.E))
                        continue;

                    // If recursive, add it to the list of entities to delete
                    if (ShouldRecursiveDelete(db, cache, reference))
                    {
                        remain.Push(reference.E);
                    }
                    else
                    {
                        // Otherwise, just delete the reference
                        tx.Add(reference.WithRetract());
                    }
                }
            }
        }
    }

    /// <summary>
    /// Decide if the entity that contains the given datom should be deleted recursively if this
    /// datom needs to be removed.
    /// </summary>
    private static bool ShouldRecursiveDelete(IDb db, AttributeCache cache, Datom referenceDatom)
    {
        // If the reference is not a collection, then we can delete the whole thing as it's a child of this entity
        // We can get more detailed, but for now we assume that if the reference is a collection, we should not delete it
        // because it's likely a root of some other system.
        return !cache.IsCardinalityMany(referenceDatom.A);
        
    }

    private readonly record struct DeleteThisOnlyFn(EntityId Eid) : ITxFunction
    {
        public void Apply(Transaction tx)
        {
            foreach (var datom in tx.BasisDb[Eid])
            {
                tx.Add(datom.WithRetract());
            }
        }
    }

}
