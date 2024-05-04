using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that represents a collection of values
/// </summary>
public abstract class CollectionAttribute<TValue, TLowLevel>(ValueTags tag, string ns, string name)
    : Attribute<TValue, TLowLevel>(tag, ns, name, cardinality: Cardinality.Many)
{

    /// <summary>
    /// Gets all values for this attribute on the given entity
    /// </summary>
    public Values<TValue, TLowLevel> Get(IEntity ent)
    {
        var segment = ent.Db.Get(ent.Id);
        var dbId = Cache[segment.RegistryId.Value];
        for (var i = 0; i < segment.Count; i++)
        {
            var datom = segment[i];
            if (datom.A != dbId) continue;

            var start = i;
            while (i < segment.Count - 1 && segment[i].A == dbId)
            {
                i++;
            }
            return new Values<TValue, TLowLevel>(segment, start, i, this);
        }
        return new Values<TValue, TLowLevel>(segment, 0, 0, this);
    }

    /// <summary>
    /// Retracts all values for this attribute on the given entity
    /// </summary>
    public void RetractAll(IEntityWithTx entityWithTx)
    {
        foreach (var value in Get(entityWithTx))
        {
            Retract(entityWithTx, value);
        }
    }

}
