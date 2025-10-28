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
        => entity.EntitySegment.Contains(this);

    /// <summary>
    ///  Tries to get the value of the attribute from the entity.
    /// </summary>
    public bool TryGetValue<T>(T segment, [NotNullWhen(true)] out TValue? value) 
        where T : IHasIdAndEntitySegment 
        => segment.EntitySegment.TryGetResolved(this, out value);


    /// <summary>
    /// Gets the value of the attribute from the entity.
    /// </summary>
    public TValue GetFrom<T>(T entity)
        where T : IHasIdAndEntitySegment
    {
        if (entity.EntitySegment.TryGetResolved(this, out var value))
            return value;
        if (DefaultValue.HasValue)
            return DefaultValue.Value;
        return ThrowKeyNotfoundException(entity);
    }
    
    
    /// <summary>
    /// Gets the value of the attribute from the entity, <see cref="DefaultValue"/>, or <see cref="Optional{TValue}.None"/>.
    /// </summary>
    public Optional<TValue> GetOptional<T>(T entity)
        where T : IHasIdAndEntitySegment
    {
        if (entity.EntitySegment.TryGetResolved(this, out var value))
            return value;
        return Optional<TValue>.None;
    }
    

    [DoesNotReturn]
    private TValue ThrowKeyNotfoundException(object entity)
    {
        throw new KeyNotFoundException($"Entity `{entity}` doesn't have attribute {Id}");
#pragma warning disable CS0162 // Unreachable code detected
        return default!;
#pragma warning restore CS0162 // Unreachable code detected
    }

    /// <summary>
    /// The default value for this attribute that is used when the attribute is not present on an entity
    /// </summary>
    public Optional<TValue> DefaultValue { get; init; }
}
