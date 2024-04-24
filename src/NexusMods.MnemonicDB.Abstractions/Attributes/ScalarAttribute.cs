using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that represents a scalar value, where there is a 1:1 ratio between the attribute and the value.
/// </summary>
public abstract class ScalarAttribute<TValue, TLowLevel>(ValueTags tag, string ns, string name) :
    Attribute<TValue, TLowLevel>(tag, ns, name)
{
    /// <summary>
    ///   Gets the value of the attribute from the entity.
    /// </summary>
    public TValue Get(IEntity entity)
    {
        var segment = entity.Db.Get(entity.Id);
        var dbId = Cache[segment.RegistryId.Value];
        for (var i = 0; i < segment.Count; i++)
        {
            var datom = segment[i];
            if (datom.A != dbId) continue;
            return ReadValue(datom.ValueSpan);
        }

        ThrowKeyNotfoundException(entity);
        return default!;
    }

    /// <summary>
    /// Retracts the attribute from the entity.
    /// </summary>
    public void Retract(IEntityWithTx entityWithTx)
    {
        Retract(entityWithTx, Get(entityWithTx));
    }

    private void ThrowKeyNotfoundException(IEntity entity)
    {
        throw new KeyNotFoundException($"Entity {entity.Id} does not have attribute {Id}");
    }

    /// <summary>
    ///   Try to get the value of the attribute from the entity.
    /// </summary>
    public bool TryGet(Entity entity, out TValue value)
    {
        var segment = entity.Db.Get(entity.Id);
        var dbId = Cache[segment.RegistryId.Value];
        for (var i = 0; i < segment.Count; i++)
        {
            var datom = segment[i];
            if (datom.A != dbId) continue;
            value = ReadValue(datom.ValueSpan);
            return true;
        }

        value = default!;
        return false;
    }
}
