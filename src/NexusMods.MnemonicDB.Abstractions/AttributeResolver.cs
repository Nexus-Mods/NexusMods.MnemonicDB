using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Occasionally we need to turn the raw datoms from the database into a IReadDatom, this class
/// provides the mappings from AttributeId to IAttribute
/// </summary>
public sealed class AttributeResolver
{
    private readonly FrozenDictionary<Symbol,IAttribute> _attrsById;
    private readonly AttributeCache _attributeCache;

    /// <summary>
    /// Occasionally we need to turn the raw datoms from the database into a IReadDatom, this class
    /// provides the mappings from AttributeId to IAttribute
    /// </summary>
    public AttributeResolver(IServiceProvider provider, AttributeCache cache)
    {
        ServiceProvider = provider;
        _attributeCache = cache;
        _attrsById = provider.GetServices<IAttribute>().ToDictionary(a => a.Id).ToFrozenDictionary();
        
        ValidateAttributes();
    }

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
    public IReadDatom Resolve(Datom datom)
    {
        var dbId = datom.A;
        var symbol = _attributeCache.GetSymbol(dbId);
        if (!_attrsById.TryGetValue(symbol, out var attr))
        {
            throw new InvalidOperationException($"Attribute {symbol} not found");
        }
        return attr.Resolve(datom.Prefix, datom.ValueSpan, this);
    }

    public bool TryGetAttribute(AttributeId id, out IAttribute attr)
    {
        if (_attrsById.TryGetValue(_attributeCache.GetSymbol(id), out var found))
        {
            attr = found;
            return true;
        }

        attr = default!;
        return false;
    }

    /// <summary>
    /// Gets the service object of the specified type.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
    
    /// <summary>
    /// The defined attributes as seen in the DI container
    /// </summary>
    public IEnumerable<IAttribute> DefinedAttributes => _attrsById.Values;
}
