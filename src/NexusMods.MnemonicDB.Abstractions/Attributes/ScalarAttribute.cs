using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that represents a scalar value, where there is a 1:1 ratio between the attribute and the value.
/// </summary>
[PublicAPI]
public abstract class ScalarAttribute<TValue, TLowLevel, TSerializer>(string ns, string name) :
    Attribute<TValue, TLowLevel, TSerializer>(ns, name)
    where TSerializer : IValueSerializer<TLowLevel>
    where TValue : notnull
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
    /// Tries to get the value of the attribute from the entity.
    /// </summary>
    public bool TryGetValue<T>(T entity, IndexSegment segment, [NotNullWhen(true)] out TValue? value)
        where T : IHasEntityIdAndDb
    {
        var attributeId = entity.Db.AttributeCache.GetAttributeId(Id);
        foreach (var datom in segment)
        {
            if (datom.A != attributeId) continue;
            value = ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, entity.Db.Connection.AttributeResolver);
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Gets the value of the attribute from the entity.
    /// </summary>
    public TValue Get<T>(T entity, IndexSegment segment)
        where T : IHasEntityIdAndDb
    {
        if (TryGetValue(entity, segment, out var value)) return value;
        return ThrowKeyNotfoundException(entity.Id);
    }

    /// <summary>
    /// Gets the value of the attribute from the entity.
    /// </summary>
    public TValue Get<T>(T entity)
        where T : IHasIdAndIndexSegment
    {
        var segment = entity.IndexSegment;
        return Get(entity, segment);
    }

    /// <summary>
    /// Gets the value of the attribute from the entity, <see cref="DefaultValue"/>, or <see cref="Optional{TValue}.None"/>.
    /// </summary>
    public Optional<TValue> GetOptional<T>(T entity)
        where T : IHasIdAndIndexSegment
    {
        if (TryGetValue(entity, entity.IndexSegment, out var value)) return value;
        return DefaultValue.HasValue ? DefaultValue : Optional<TValue>.None;
    }

    /// <summary>
    /// Retracts the attribute from the entity.
    /// </summary>
    public void Retract(IAttachedEntity entityWithTx)
    {
        Retract(entityWithTx, value: Get(entityWithTx, segment: entityWithTx.Db.Get(entityWithTx.Id)));
    }

    [DoesNotReturn]
    private TValue ThrowKeyNotfoundException(EntityId entityId)
    {
        throw new KeyNotFoundException($"Entity `{entityId}` doesn't have attribute {Id}");
        return default!;
    }

    /// <summary>
    /// The default value for this attribute that is used when the attribute is not present on an entity
    /// </summary>
    public Optional<TValue> DefaultValue { get; init; }
}
