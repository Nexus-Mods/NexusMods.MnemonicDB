using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// The authoritative source of attribute definitions is the database itself, this class provides a cache
/// of these definitions for use in the rest of the code routines
/// </summary>
public sealed class AttributeCache
{
    private Dictionary<Symbol, AttributeId> _attributeIdsBySymbol = new();
    private readonly BitArray _isCardinalityMany;
    private readonly BitArray _isReference;
    private readonly BitArray _isIndexed;
    private readonly Symbol[] _symbols;

    public AttributeCache()
    {
        var maxId = AttributeDefinition.HardcodedIds.Values.Max() + 1;
        _isCardinalityMany = new BitArray(maxId);
        _isReference = new BitArray(maxId);
        _isIndexed = new BitArray(maxId);
        _symbols = new Symbol[maxId];

        foreach (var kv in AttributeDefinition.HardcodedIds)
        {
            _attributeIdsBySymbol[kv.Key.Id] = AttributeId.From(kv.Value);
            _isIndexed[kv.Value] = kv.Key.IsIndexed;
            _symbols[kv.Value] = kv.Key.Id;
        }
         
    }
    
    /// <summary>
    /// Resets the cache, causing it to re-query the database for the latest definitions.
    /// </summary>
    /// <param name="idb"></param>
    public void Reset(IDb idb)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Returns true if the attribute is a reference attribute.
    /// </summary>
    public bool IsReference(AttributeId attrId)
    {
        return _isReference[attrId.Value];
    }

    /// <summary>
    /// Returns true if the attribute is indexed.
    /// </summary>
    public bool IsIndexed(AttributeId attrId)
    {
        return _isIndexed[attrId.Value];
    }

    /// <summary>
    /// Returns true if the attribute is `NoHistory`.
    /// </summary>
    public bool IsNoHistory(AttributeId attrId)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Returns true if the attribute cardinality is many.
    /// </summary>
    public bool IsCardinalityMany(AttributeId attrId)
    {
        return _isCardinalityMany[attrId.Value];
    }

    /// <summary>
    /// Get the AttributeId (DB attribute id) for the given attribute name
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AttributeId GetAttributeId(Symbol attribute)
    {
        return _attributeIdsBySymbol[attribute];
    }

    public Symbol GetSymbol(AttributeId id)
    {
        return _symbols[id.Value];
    }
}
