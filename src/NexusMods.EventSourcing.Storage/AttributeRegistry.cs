using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage;

/// <summary>
/// Tracks all attributes and their respective serializers as well as the DB entity IDs for each
/// attribute
/// </summary>
public class AttributeRegistry : IAttributeRegistry
{
    private readonly Dictionary<Type,IValueSerializer> _valueSerializersByNativeType;
    private readonly Dictionary<Symbol,IAttribute> _attributesById;
    private readonly Dictionary<Type,IAttribute> _attributesByType;
    private readonly Dictionary<AttributeId,DbAttribute> _dbAttributesByEntityId;
    private readonly Dictionary<Symbol,DbAttribute> _dbAttributesByUniqueId;
    private readonly Dictionary<Symbol,IValueSerializer> _valueSerializersByUniqueId;
    private readonly Dictionary<AttributeId, IAttribute> _attributesByAttributeId;

    /// <summary>
    /// Tracks all attributes and their respective serializers as well as the DB entity IDs for each
    /// attribute
    /// </summary>
    public AttributeRegistry(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    {
        var serializers = valueSerializers.ToArray();
        _valueSerializersByNativeType = serializers.ToDictionary(x => x.NativeType);
        _valueSerializersByUniqueId = serializers.ToDictionary(x => x.UniqueId);

        var attributeArray = attributes.ToArray();
        _attributesById = attributeArray.ToDictionary(x => x.Id);
        _attributesByType = attributeArray.ToDictionary(x => x.GetType());
        _attributesByAttributeId = new();

        foreach (var attr in attributeArray)
        {
            if (!_valueSerializersByNativeType.TryGetValue(attr.ValueType, out var serializer))
                throw new InvalidOperationException($"No serializer found for type {attr.ValueType}");

            attr.SetSerializer(serializer);
        }

        _dbAttributesByEntityId = new Dictionary<AttributeId, DbAttribute>();
        _dbAttributesByUniqueId = new Dictionary<Symbol, DbAttribute>();
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
    public void WriteValue<TWriter, TVal>(TVal val, in TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        if (!_valueSerializersByNativeType.TryGetValue(typeof(TVal), out var serializer))
            throw new InvalidOperationException($"No serializer found for type {typeof(TVal)}");

        ((IValueSerializer<TVal>)serializer).Serialize(val, writer);
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

    public AttributeId GetAttributeId(Type datomAttributeType)
    {
        if (!_attributesByType.TryGetValue(datomAttributeType, out var attribute))
            throw new InvalidOperationException($"No attribute found for type {datomAttributeType}");

        if (!_dbAttributesByUniqueId.TryGetValue(attribute.Id, out var dbAttribute))
            throw new InvalidOperationException($"No DB attribute found for attribute {attribute}");

        return dbAttribute.AttrEntityId;
    }


    public int CompareValues(in Datom a, in Datom b)
    {
        var attr = _dbAttributesByEntityId[a.A];
        var type = _valueSerializersByUniqueId[attr.ValueTypeId];
        return type.Compare(a.V.Span, b.V.Span);
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

    private sealed class CompareCache
    {
        public AttributeId AttributeId;
        public IValueSerializer Serializer = null!;
    }

    private CompareCache _compareCache = new();
    public int CompareValues<T>(T datomsValues, AttributeId attributeId, int a, int b) where T : IBlobColumn
    {
        var cache = _compareCache;
        if (cache.AttributeId == attributeId)
            return cache.Serializer.Compare(datomsValues[a].Span, datomsValues[b].Span);

        var attr = _dbAttributesByEntityId[attributeId];
        var type = _valueSerializersByUniqueId[attr.ValueTypeId];
        _compareCache = new CompareCache {AttributeId = attributeId, Serializer = type};
        return type.Compare(datomsValues[a].Span, datomsValues[b].Span);
    }

    public int CompareValues(AttributeId id, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length == 0 || b.Length == 0)
        {
            return a.Length < b.Length ? -1 : a.Length > b.Length ? 1 : 0;
        }

        var cache = _compareCache;
        if (cache.AttributeId == id)
            return cache.Serializer.Compare(a, b);

        var attr = _dbAttributesByEntityId[id];
        var type = _valueSerializersByUniqueId[attr.ValueTypeId];
        _compareCache = new CompareCache {AttributeId = id, Serializer = type};
        return type.Compare(a, b);
    }

    public void Explode<TAttribute, TValueType, TBufferWriter>(out AttributeId id, TValueType value, TBufferWriter writer)
        where TAttribute : IAttribute<TValueType>
        where TBufferWriter : IBufferWriter<byte>
    {
        var attr = _attributesByType[typeof(TAttribute)];
        var dbAttr = _dbAttributesByUniqueId[attr.Id];
        var serializer = (IValueSerializer<TValueType>)_valueSerializersByUniqueId[dbAttr.ValueTypeId];
        id = dbAttr.AttrEntityId;
        serializer.Serialize(value, writer);
    }

    public IReadDatom Resolve(Datom datom)
    {
        if (!_dbAttributesByEntityId.TryGetValue(datom.A, out var dbAttr))
            throw new InvalidOperationException($"No attribute found for entity ID {datom.A}");

        if (!_attributesById.TryGetValue(dbAttr.UniqueId, out var attr))
            throw new InvalidOperationException($"No attribute found for unique ID {dbAttr.UniqueId}");

        return attr.Resolve(datom);
    }

    public IReadDatom Resolve(EntityId entityId, AttributeId attributeId, ReadOnlySpan<byte> value, TxId tx)
    {
        if (!_attributesByAttributeId.TryGetValue(attributeId, out var attr))
            throw new InvalidOperationException($"No attribute found for AttributeId {attributeId}");

        return attr.Resolve(entityId, attributeId, value, tx);
    }

    public bool IsReference(AttributeId attributeId)
    {
        var dbAttr = _dbAttributesByEntityId[attributeId];
        var attrobj = _attributesById[dbAttr.UniqueId];
        return attrobj.IsReference;
    }

    public IValueSerializer<TValueType> GetSerializer<TValueType>()
    {
        if (!_valueSerializersByNativeType.TryGetValue(typeof(TValueType), out var serializer))
            throw new InvalidOperationException($"No serializer found for type {typeof(TValueType)}");

        return (IValueSerializer<TValueType>)serializer;
    }

    public TValue Read<TAttr, TValue>(ReadOnlySpan<byte> tValueSpan) where TAttr : IAttribute<TValue>
    {
        var attr = _attributesByType[typeof(TAttr)];
        var dbAttr = _dbAttributesByUniqueId[attr.Id];
        var serializer = (IValueSerializer<TValue>)_valueSerializersByUniqueId[dbAttr.ValueTypeId];
        serializer.Read(tValueSpan, out var val);
        return val;
    }

    public Type GetReadDatomType(Type attribute)
    {
        var attr = _attributesByType[attribute];
        return attr.GetReadDatomType();
    }

}
