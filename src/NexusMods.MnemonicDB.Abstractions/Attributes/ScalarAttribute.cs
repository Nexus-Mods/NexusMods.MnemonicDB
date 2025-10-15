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
    where TLowLevel : notnull
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
    /// True whether the index segment contains this attribute.
    /// </summary>
    public bool Contains<T>(T entity) where T : IHasIdAndEntitySegment
    {
        return entity.EntitySegment.Contains(this);
    }

    /// <summary>
    ///  Tries to get the value of the attribute from the entity.
    /// </summary>
    public bool TryGetValue(EntitySegment segment, [NotNullWhen(true)] out TValue? value)
    {
        var attributeId = segment.Db.AttributeCache.GetAttributeId(Id);
        return segment.TryGetValue(this, attributeId, out value);
    }

    /// <summary>
    /// Tries to get the value of the attribute from the entity.
    /// </summary>
    public bool TryGetValue<T>(T entity, [NotNullWhen(true)] out TValue? value)
        where T : IHasIdAndEntitySegment
    {
        return TryGetValue(segment: entity.EntitySegment, out value);
    }

    /// <summary>
    /// Gets the value of the attribute from the entity.
    /// </summary>
    public TValue Get<T>(T entity, EntitySegment segment)
        where T : IHasEntityIdAndDb
    {
        if (TryGetValue(segment, out var value)) 
            return value;
        if (DefaultValue.HasValue) 
            return DefaultValue.Value;
        return ThrowKeyNotfoundException(entity.Id);
    }

    /// <summary>
    /// Gets the value of the attribute from the entity.
    /// </summary>
    public TValue Get<T>(T entity)
        where T : IHasIdAndEntitySegment
    {
        var aid = entity.Db.AttributeCache.GetAttributeId(Id);
        if (!entity.EntitySegment.TryGetValue<ScalarAttribute<TValue, TLowLevel, TSerializer>, TValue>(this, aid, out var value))
            return DefaultValue.HasValue ? DefaultValue.Value : ThrowKeyNotfoundException(entity.Id);
        return value;
    }

    /// <summary>
    /// Gets the value of the attribute from the entity, <see cref="DefaultValue"/>, or <see cref="Optional{TValue}.None"/>.
    /// </summary>
    public Optional<TValue> GetOptional<T>(T entity)
        where T : IHasIdAndEntitySegment
    {
        var aid = entity.Db.AttributeCache.GetAttributeId(Id);
        if (entity.EntitySegment.TryGetValue<ScalarAttribute<TValue, TLowLevel, TSerializer>, TValue>(this, aid, out var value))
            return value;
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
#pragma warning disable CS0162 // Unreachable code detected
        return default!;
#pragma warning restore CS0162 // Unreachable code detected
    }

    /// <summary>
    /// The default value for this attribute that is used when the attribute is not present on an entity
    /// </summary>
    public Optional<TValue> DefaultValue { get; init; }
}
