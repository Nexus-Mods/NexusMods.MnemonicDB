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
    /// True if the attribute is optional, and not required by models
    /// </summary>
    public bool IsOptional
    {
        get => DeclaredOptional;
        init => DeclaredOptional = value;
    }

    /// <summary>
    ///   Gets the value of the attribute from the entity.
    /// </summary>
    public TValue Get(IHasEntityIdAndDb entity)
    {
        var segment = entity.Db.Get(entity.Id);
        var dbId = Cache[segment.RegistryId.Value];
        for (var i = 0; i < segment.Count; i++)
        {
            var datom = segment[i];
            if (datom.A != dbId) continue;
            return ReadValue(datom.ValueSpan, datom.Prefix.ValueTag);
        }

        if (DefaultValue is not null)
            return DefaultValue;

        ThrowKeyNotfoundException(entity);
        return default!;
    }

    /// <summary>
    /// Retracts the attribute from the entity.
    /// </summary>
    public void Retract(IAttachedEntity entityWithTx)
    {
        Retract(entityWithTx, Get(entityWithTx));
    }

    private void ThrowKeyNotfoundException(IHasEntityIdAndDb entity)
    {
        throw new KeyNotFoundException($"Entity {entity.Id} does not have attribute {Id}");
    }

    /// <summary>
    ///   Try to get the value of the attribute from the entity.
    /// </summary>
    public bool TryGet(IHasEntityIdAndDb entity, out TValue value)
    {
        var segment = entity.Db.Get(entity.Id);
        var dbId = Cache[segment.RegistryId.Value];
        for (var i = 0; i < segment.Count; i++)
        {
            var datom = segment[i];
            if (datom.A != dbId) continue;
            value = ReadValue(datom.ValueSpan, datom.Prefix.ValueTag);
            return true;
        }

        if (DefaultValue is not null)
        {
            value = DefaultValue;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// The default value for this attribute that is used when the attribute is not present on an entity
    /// </summary>
    public TValue? DefaultValue { get; init; }
}
