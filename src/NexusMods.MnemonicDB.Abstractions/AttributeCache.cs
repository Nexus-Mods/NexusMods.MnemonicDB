using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// The authoritative source of attribute definitions is the database itself, this class provides a cache
/// of these definitions for use in the rest of the code routines
/// </summary>
public sealed class AttributeCache
{
    private Dictionary<Symbol, AttributeId> _attributeIdsBySymbol = new();
    private BitArray _isCardinalityMany;
    private BitArray _isReference;
    private BitArray _isIndexed;
    private BitArray _isUnique;
    private Symbol[] _symbols;
    private ValueTag[] _valueTags;
    private BitArray _isNoHistory;

    /// <summary>
    /// Constructs a new cache, populated with the hardcoded attribute definitions
    /// </summary>
    public AttributeCache()
    {
        var maxId = AttributeDefinition.HardcodedIds.Values.Max() + 1;
        _isCardinalityMany = new BitArray(maxId);
        _isReference = new BitArray(maxId);
        _isIndexed = new BitArray(maxId);
        _isUnique = new BitArray(maxId);
        _isNoHistory = new BitArray(maxId);
        _symbols = new Symbol[maxId];
        _valueTags = new ValueTag[maxId];

        foreach (var kv in AttributeDefinition.HardcodedIds)
        {
            _attributeIdsBySymbol[kv.Key.Id] = AttributeId.From(kv.Value);
            _isIndexed[kv.Value] = kv.Key.IsIndexed;
            _isUnique[kv.Value] = kv.Key.IsUnique;
            _symbols[kv.Value] = kv.Key.Id;
            _valueTags[kv.Value] = kv.Key.LowLevelType;
        }
         
    }

    /// <summary>
    /// All the defined attribute ids
    /// </summary>
    public IEnumerable<Symbol> AllAttributeIds => _attributeIdsBySymbol.Keys;

    /// <summary>
    /// Resets the cache, causing it to re-query the database for the latest definitions.
    /// </summary>
    public void Reset(IDb db)
    {
        var symbols = db.Datoms(AttributeDefinition.UniqueId);
        var maxIndex = (int)symbols.MaxBy(static x => x.E.Value).E.Value + 1;

        var newSymbols = new Symbol[maxIndex];
        foreach (var datom in symbols)
        {
            var id = datom.E.Value;
            var symbol = AttributeDefinition.UniqueId.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, null!);
            newSymbols[id] = symbol;
            _attributeIdsBySymbol[symbol] = AttributeId.From((ushort)id);
        }
        _symbols = newSymbols;
        
        var types = db.Datoms(AttributeDefinition.ValueType);
        var newTypes = new ValueTag[maxIndex];
        var newIsReference = new BitArray(maxIndex);
        foreach (var datom in types)
        {
            var id = datom.E.Value;
            var type = AttributeDefinition.ValueType.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, null!);
            newTypes[id] = type;
            newIsReference[(int)id] = type == ValueTag.Reference;
        }
        _isReference = newIsReference;

        var isIndexed = db.Datoms(AttributeDefinition.Indexed);
        var newIsIndexed = new BitArray(maxIndex);
        foreach (var datom in isIndexed)
        {
            var id = datom.E.Value;
            newIsIndexed[(int)id] = true;
        }
        _isIndexed = newIsIndexed;
        
        var isUnique = db.Datoms(AttributeDefinition.Unique);
        var newIsUnique = new BitArray(maxIndex);
        foreach (var datom in isUnique)
        {
            var id = datom.E.Value;
            newIsUnique[(int)id] = true;
        }
        _isUnique = newIsUnique;
        
        var isNoHistory = db.Datoms(AttributeDefinition.NoHistory);
        var newIsNoHistory = new BitArray(maxIndex);
        if (isNoHistory.Any())
        {
            foreach (var datom in isNoHistory)
            {
                var id = datom.E.Value;
                newIsNoHistory[(int)id] = true;
            }
        }
        _isNoHistory = newIsNoHistory;

        var isCardinalityMany = db.Datoms(AttributeDefinition.Cardinality);
        var newIsCardinalityMany = new BitArray(maxIndex);
        foreach (var datom in isCardinalityMany)
        {
            var id = datom.E.Value;
            newIsCardinalityMany[(int)id] = AttributeDefinition.Cardinality.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, null!) == Cardinality.Many;
        }
        _isCardinalityMany = newIsCardinalityMany;
        
        var valueTags = db.Datoms(AttributeDefinition.ValueType);
        var newValueTags = new ValueTag[maxIndex];
        foreach (var datom in valueTags)
        {
            var id = datom.E.Value;
            var type = AttributeDefinition.ValueType.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, null!);
            newValueTags[id] = type;
        }
        _valueTags = newValueTags;
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
        return _isNoHistory[attrId.Value];
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

    /// <summary>
    /// Get the symbol for the given attribute id
    /// </summary>
    public Symbol GetSymbol(AttributeId id)
    {
        return _symbols[id.Value];
    }

    /// <summary>
    /// Returns true if the attribute is defined in the database.
    /// </summary>
    public bool Has(Symbol attribute)
    {
        return _attributeIdsBySymbol.ContainsKey(attribute);
    }
    
    /// <summary>
    /// Try to get the AttributeId for the given attribute name
    /// </summary>
    public bool TryGetAttributeId(Symbol attribute, out AttributeId id)
    {
        return _attributeIdsBySymbol.TryGetValue(attribute, out id);
    }

    /// <summary>
    /// Return the value tag type for the given attribute id
    /// </summary>
    public ValueTag GetValueTag(AttributeId aid)
    {
        return _valueTags[aid.Value];
    }
}
