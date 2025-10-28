using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Occasionally we need to turn the raw datoms from the database into a IReadDatom, this class
/// provides the mappings from AttributeId to IAttribute
/// </summary>
public sealed class AttributeResolver
{
    private readonly FrozenDictionary<Symbol,IAttribute> _attrsById;
    public readonly AttributeCache AttributeCache;

    /// <summary>
    /// Occasionally we need to turn the raw datoms from the database into a IReadDatom, this class
    /// provides the mappings from AttributeId to IAttribute
    /// </summary>
    public AttributeResolver(IServiceProvider provider, AttributeCache cache)
    {
        ServiceProvider = provider;
        AttributeCache = cache;
        _attrsById = provider.GetServices<IAttribute>().ToDictionary(a => a.Id).ToFrozenDictionary();
        
        ValidateAttributes();
    }
    
    /// <summary>
    /// Get the attribute id for the given attribute.
    /// </summary>
    public AttributeId this[IAttribute attr] => AttributeCache.GetAttributeId(attr.Id);

    private void ValidateAttributes()
    {
        foreach (var attribute in _attrsById.Values)
        {
            if (attribute.IsUnique && !attribute.IsIndexed)
            {
                throw new InvalidOperationException($"Attribute {attribute.Id} is unique but not indexed, all unique attributes must also be indexed");
            }
        }
    }


    /// <summary>
    /// Resolves a datom into a IReadDatom
    /// </summary>
    public ResolvedDatom Resolve(Datom datom) 
        => new(datom, this);

    public bool TryGetAttribute(AttributeId id, out IAttribute attr)
    {
        if (_attrsById.TryGetValue(AttributeCache.GetSymbol(id), out var found))
        {
            attr = found;
            return true;
        }

        attr = default!;
        return false;
    }
    
    /// <summary>
    /// Get an attribute by its id.
    /// </summary>
    /// <param name="id"></param>
    public IAttribute this[AttributeId id] => _attrsById[AttributeCache.GetSymbol(id)];

    /// <summary>
    /// Gets the service object of the specified type.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
    
    /// <summary>
    /// The defined attributes as seen in the DI container
    /// </summary>
    public IEnumerable<IAttribute> DefinedAttributes => _attrsById.Values;
}
