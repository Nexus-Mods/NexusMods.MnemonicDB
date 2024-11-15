using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Iterators;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Occasionally we need to turn the raw datoms from the database into a IReadDatom, this class
/// provides the mappings from AttributeId to IAttribute
/// </summary>
public sealed class AttributeResolver
{
    private readonly FrozenDictionary<Symbol,IAttribute> _attrsById;
    private AttributeCache _attributeCache;

    /// <summary>
    /// Occasionally we need to turn the raw datoms from the database into a IReadDatom, this class
    /// provides the mappings from AttributeId to IAttribute
    /// </summary>
    public AttributeResolver(IServiceProvider provider, AttributeCache cache)
    {
        ServiceProvider = provider;
        _attributeCache = cache;
        _attrsById = provider.GetServices<IAttribute>().ToDictionary(a => a.Id).ToFrozenDictionary();
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
    
    /// <summary>
    /// Resolves a datom into a IReadDatom
    /// </summary>
    public IReadDatom Resolve(in RefDatom datom)
    {
        var dbId = datom.A;
        var symbol = _attributeCache.GetSymbol(dbId);
        if (!_attrsById.TryGetValue(symbol, out var attr))
        {
            throw new InvalidOperationException($"Attribute {symbol} not found");
        }
        return attr.Resolve(datom.Prefix, datom.ValueSpan, this);
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
