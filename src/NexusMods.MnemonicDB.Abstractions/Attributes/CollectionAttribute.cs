using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that represents a collection of values
/// </summary>
[PublicAPI]
public abstract class CollectionAttribute<TValue, TLowLevel, TSerializer>(string ns, string name)
    : Attribute<TValue, TLowLevel, TSerializer>(ns, name, cardinality: Cardinality.Many) 
    where TSerializer : IValueSerializer<TLowLevel>
{

    /// <summary>
    /// Gets all values for this attribute on the given entity
    /// </summary>
    public Values<TValue> Get(IHasIdAndIndexSegment ent)
    {
        var segment = ent.IndexSegment;
        var dbId = ent.Db.AttributeCache.GetAttributeId(Id);
        for (var i = 0; i < segment.Count; i++)
        {
            var datom = segment[i];
            if (datom.A != dbId) continue;

            var start = i;
            while (i < segment.Count && segment[i].A == dbId)
            {
                i++;
            }
            return new Values<TValue>(segment, start, i, this, ent.Db.Connection.AttributeResolver);
        }
        return new Values<TValue>(segment, 0, 0, this, ent.Db.Connection.AttributeResolver);
    }
    
    /// <summary>
    /// Gets all values for this attribute on the given entity, this performs a lookup in the database
    /// so prefer using the overload with IHasIdAndIndexSegment if you already have the segment.
    /// </summary>
    protected Values<TValue> Get(IHasEntityIdAndDb ent)
    {
        var segment = ent.Db.Get(ent.Id);
        var dbId = ent.Db.AttributeCache.GetAttributeId(Id);
        for (var i = 0; i < segment.Count; i++)
        {
            var datom = segment[i];
            if (datom.A != dbId) continue;

            var start = i;
            while (i < segment.Count && segment[i].A == dbId)
            {
                i++;
            }
            return new Values<TValue>(segment, start, i, this, ent.Db.Connection.AttributeResolver);
        }
        return new Values<TValue>(segment, 0, 0, this, ent.Db.Connection.AttributeResolver);
    }

    /// <summary>
    /// Retracts all values for this attribute on the given entity
    /// </summary>
    public void RetractAll(IAttachedEntity entityWithTx)
    {
        foreach (var value in Get(entityWithTx))
        {
            Retract(entityWithTx, value);
        }
    }

}
