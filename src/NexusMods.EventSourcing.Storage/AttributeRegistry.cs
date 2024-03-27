using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage;

/// <summary>
///     Tracks all attributes and their respective serializers as well as the DB entity IDs for each
///     attribute
/// </summary>
public class AttributeRegistry : IAttributeRegistry
{
    private readonly Dictionary<AttributeId, IAttribute> _attributesByAttributeId;
    private readonly Dictionary<Symbol, IAttribute> _attributesById;
    private readonly Dictionary<Type, IAttribute> _attributesByType;
    private readonly Dictionary<AttributeId, DbAttribute> _dbAttributesByEntityId;
    private readonly Dictionary<Symbol, DbAttribute> _dbAttributesByUniqueId;
    private readonly Dictionary<Type, IValueSerializer> _valueSerializersByNativeType;
    private readonly Dictionary<Symbol, IValueSerializer> _valueSerializersByUniqueId;

    private CompareCache _compareCache = new();

    /// <summary>
    ///     Tracks all attributes and their respective serializers as well as the DB entity IDs for each
    ///     attribute
    /// </summary>
    public AttributeRegistry(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    {
        var serializers = valueSerializers.ToArray();
        _valueSerializersByNativeType = serializers.ToDictionary(x => x.NativeType);
        _valueSerializersByUniqueId = serializers.ToDictionary(x => x.UniqueId);

        var attributeArray = attributes.ToArray();
        _attributesById = attributeArray.ToDictionary(x => x.Id);
        _attributesByType = attributeArray.ToDictionary(x => x.GetType());
        _attributesByAttributeId = new Dictionary<AttributeId, IAttribute>();

        foreach (var attr in attributeArray)
        {
            if (!_valueSerializersByNativeType.TryGetValue(attr.ValueType, out var serializer))
                throw new InvalidOperationException($"No serializer found for type {attr.ValueType}");

            attr.SetSerializer(serializer);
        }

        _dbAttributesByEntityId = new Dictionary<AttributeId, DbAttribute>();
        _dbAttributesByUniqueId = new Dictionary<Symbol, DbAttribute>();
    }

    public AttributeId GetAttributeId(Type datomAttributeType)
    {
        if (!_attributesByType.TryGetValue(datomAttributeType, out var attribute))
            throw new InvalidOperationException($"No attribute found for type {datomAttributeType}");

        if (!_dbAttributesByUniqueId.TryGetValue(attribute.Id, out var dbAttribute))
            throw new InvalidOperationException($"No DB attribute found for attribute {attribute}");

        return dbAttribute.AttrEntityId;
    }

    public IReadDatom Resolve(ReadOnlySpan<byte> datom)
    {
        var c = MemoryMarshal.Read<KeyPrefix>(datom);
        if (!_attributesByAttributeId.TryGetValue(c.A, out var attribute))
            throw new InvalidOperationException($"No attribute found for attribute ID {c.A}");

        unsafe
        {
            return attribute.Resolve(c.E, c.A, datom.SliceFast(sizeof(KeyPrefix)), c.T, c.IsRetract);
        }
    }

    public int CompareValues(AttributeId id, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length == 0 || b.Length == 0) return a.Length < b.Length ? -1 : a.Length > b.Length ? 1 : 0;

        var cache = _compareCache;
        if (cache.AttributeId == id)
            return cache.Serializer.Compare(a, b);

        var attr = _dbAttributesByEntityId[id];
        var type = _valueSerializersByUniqueId[attr.ValueTypeId];
        _compareCache = new CompareCache { AttributeId = id, Serializer = type };
        return type.Compare(a, b);
    }

    public void Explode<TAttribute, TValueType, TBufferWriter>(out AttributeId id, TValueType value,
        TBufferWriter writer)
        where TAttribute : IAttribute<TValueType>
        where TBufferWriter : IBufferWriter<byte>
    {
        var attr = _attributesByType[typeof(TAttribute)];
        var dbAttr = _dbAttributesByUniqueId[attr.Id];
        var serializer = (IValueSerializer<TValueType>)_valueSerializersByUniqueId[dbAttr.ValueTypeId];
        id = dbAttr.AttrEntityId;
        serializer.Serialize(value, writer);
    }

    public Symbol GetSymbolForAttribute(Type attribute)
    {
        if (!_attributesByType.TryGetValue(attribute, out var attr))
            throw new InvalidOperationException($"No attribute found for type {attribute}");

        return attr.Id;
    }

    public void Populate(DbAttribute[] attributes)
    {
        foreach (var attr in attributes)
        {
            _dbAttributesByEntityId[attr.AttrEntityId] = attr;
            _dbAttributesByUniqueId[attr.UniqueId] = attr;
        }

        _attributesByAttributeId.Clear();

        foreach (var (id, dbAttr) in _dbAttributesByEntityId)
        {
            var attr = _attributesById[dbAttr.UniqueId];
            _attributesByAttributeId[id] = attr;
        }
    }

    public AttributeId GetAttributeId<TAttr>()
        where TAttr : IAttribute
    {
        if (!_attributesByType.TryGetValue(typeof(TAttr), out var attribute))
            throw new InvalidOperationException($"No attribute found for type {typeof(TAttr)}");

        if (!_dbAttributesByUniqueId.TryGetValue(attribute.Id, out var dbAttribute))
            throw new InvalidOperationException($"No DB attribute found for attribute {attribute}");

        return dbAttribute.AttrEntityId;
    }

    public Expression GetReadExpression(Type attributeType, Expression valueSpan, out AttributeId attributeId)
    {
        var attr = _attributesByType[attributeType];
        attributeId = _dbAttributesByUniqueId[attr.Id].AttrEntityId;
        var serializer = _valueSerializersByNativeType[attr.ValueType];
        var readMethod = serializer.GetType().GetMethod("Read")!;
        var valueExpr = Expression.Parameter(attr.ValueType, "retVal");
        var readExpression = Expression.Call(Expression.Constant(serializer), readMethod, valueSpan, valueExpr);
        return Expression.Block([valueExpr], readExpression, valueExpr);
    }

    public Type GetReadDatomType(Type attribute)
    {
        var attr = _attributesByType[attribute];
        return attr.GetReadDatomType();
    }

    public IAttribute GetAttribute(AttributeId attributeId)
    {
        if (!_attributesByAttributeId.TryGetValue(attributeId, out var attr))
            throw new InvalidOperationException($"No attribute found for AttributeId {attributeId}");

        return attr;
    }

    private sealed class CompareCache
    {
        public AttributeId AttributeId;
        public IValueSerializer Serializer = null!;
    }
}
