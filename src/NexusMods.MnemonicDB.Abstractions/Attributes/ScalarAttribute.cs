using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using JetBrains.Annotations;
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
    public bool TryGetValue(Datoms segment, AttributeResolver resolver, [NotNullWhen(true)] out TValue? value) 
    {
        var attributeId = segment.AttributeCache.GetAttributeId(Id);
        if (segment.TryGetOne(this, resolver, out var foundValue))
        {
            value = (TValue)foundValue;
            return true;
        }
        value = default;
        return false;
    }


    /// <summary>
    /// Gets the value of the attribute from the entity.
    /// </summary>
    public TValue Get<T>(T entity, Datoms segment)
        where T : IHasEntityIdAndDb
    {
        if (TryGetValue(segment, entity.Db.Connection.AttributeResolver, out var value)) 
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
        var resolver = entity.Db.Connection.AttributeResolver;
        if (!entity.EntitySegment.TryGetOne(this, resolver, out var value))
            return DefaultValue.HasValue ? DefaultValue.Value : ThrowKeyNotfoundException(entity.Id);
        return (TValue)value;
    }

    /// <summary>
    /// Gets the value of the attribute from the entity, <see cref="DefaultValue"/>, or <see cref="Optional{TValue}.None"/>.
    /// </summary>
    public Optional<TValue> GetOptional<T>(T entity)
        where T : IHasIdAndEntitySegment
    {
        var resolver = entity.Db.Connection.AttributeResolver;
        if (entity.EntitySegment.TryGetOne(this, resolver, out var value))
            return (TValue)value;
        return DefaultValue.HasValue ? DefaultValue : Optional<TValue>.None;
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
