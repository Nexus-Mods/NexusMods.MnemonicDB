using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Nodes;

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

        foreach (var attr in attributeArray)
        {
            attr.SetSerializer(_valueSerializersByNativeType[attr.ValueType]);
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

    public int CompareValues(in Datom a, in Datom b)
    {
        var attr = _dbAttributesByEntityId[a.A];
        var type = _valueSerializersByUniqueId[attr.ValueTypeId];
        return type.Compare(a, b);
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

    public void Append<TAttribute, TValue>(AppendableChunk chunk, EntityId entityId, TxId tx, DatomFlags flags, TValue value)
    where TAttribute : IAttribute<TValue>
    {
        var serializer = (IValueSerializer<TValue>)_valueSerializersByNativeType[typeof(TValue)];
        var attr = _attributesByType[typeof(TAttribute)];
        var dbAttr = _dbAttributesByUniqueId[attr.Id];
        chunk.Append(entityId, dbAttr.AttrEntityId, tx, flags, serializer, value);
    }

    public int CompareValues<T>(T datomsValues, AttributeId attributeId, int a, int b) where T : IBlobColumn
    {
        var attr = _dbAttributesByEntityId[attributeId];
        var type = _valueSerializersByUniqueId[attr.ValueTypeId];

        var dA = new Datom
        {
            V = datomsValues[a]
        };

        var dB = new Datom
        {
            V = datomsValues[b]
        };

        return type.Compare(dA, dB);
    }

    public void Append<TAttribute, TValue>(IAppendableChunk chunk, EntityId e, TValue value, TxId t, DatomFlags f) where TAttribute : IAttribute<TValue>
    {
        var serializer = (IValueSerializer<TValue>)_valueSerializersByNativeType[typeof(TValue)];
        var attr = _attributesByType[typeof(TAttribute)];
        var dbAttr = _dbAttributesByUniqueId[attr.Id];
        chunk.Append(e, dbAttr.AttrEntityId, t, f, serializer, value);
    }
}
