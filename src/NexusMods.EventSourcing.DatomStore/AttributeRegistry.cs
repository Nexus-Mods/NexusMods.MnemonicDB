﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore;

/// <summary>
/// Tracks all attributes and their respective serializers as well as the DB entity IDs for each
/// attribute
/// </summary>
public class AttributeRegistry
{
    private readonly Dictionary<Type,IValueSerializer> _valueSerializersByNativeType;
    private readonly Dictionary<Symbol,IAttribute> _attributesById;
    private readonly Dictionary<Type,IAttribute> _attributesByType;
    private readonly Dictionary<ulong,DbAttribute> _dbAttributesByEntityId;
    private readonly Dictionary<Symbol,DbAttribute> _dbAttributesByUniqueId;
    private readonly Dictionary<UInt128,IValueSerializer> _valueSerializersByUniqueId;

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

        _dbAttributesByEntityId = new Dictionary<ulong, DbAttribute>();
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

        ((IValueSerializer<TVal>) serializer).Write(val, writer);
    }

    public ulong GetAttributeId<TAttr>()
    where TAttr : IAttribute
    {
        if (!_attributesByType.TryGetValue(typeof(TAttr), out var attribute))
            throw new InvalidOperationException($"No attribute found for type {typeof(TAttr)}");

        if (!_dbAttributesByUniqueId.TryGetValue(attribute.Id, out var dbAttribute))
            throw new InvalidOperationException($"No DB attribute found for attribute {attribute}");

        return dbAttribute.AttrEntityId;
    }

    public unsafe int CompareValues(ulong attrId, void* aVal, uint aLength, void* bVal, uint bLength)
    {
        var attr = _dbAttributesByEntityId[attrId];
        var type = _valueSerializersByUniqueId[attr.ValueTypeId];
        return type.Compare(new ReadOnlySpan<byte>(aVal, (int) aLength), new ReadOnlySpan<byte>(bVal, (int) bLength));
    }

    public IDatom ReadDatom(ref KeyHeader header, ReadOnlySpan<byte> valueSpan)
    {
        var attrId = header.AttributeId;
        var dbAttribute = _dbAttributesByEntityId[attrId];
        var attribute = _attributesById[dbAttribute.UniqueId];
        return attribute.Read(header.Entity, header.Tx, header.IsAssert, valueSpan);
    }

    public TValue ReadValue<TAttribute, TValue>(ref KeyHeader currentHeader, ReadOnlySpan<byte> currentValue)
        where TAttribute : IAttribute<TValue>
    {
        var attrId = currentHeader.AttributeId;
        var dbAttribute = _dbAttributesByEntityId[attrId];
        var attribute = (TAttribute)_attributesById[dbAttribute.UniqueId];
        return attribute.Read(currentValue);
    }

    public Expression GetReadExpression(Type attributeType, Expression valueSpan, out ulong attributeId)
    {
        var attr = _attributesByType[attributeType];
        attributeId = _dbAttributesByUniqueId[attr.Id].AttrEntityId;
        var serializer = _valueSerializersByNativeType[attr.ValueType];
        var readMethod = serializer.GetType().GetMethod("Read")!;
        var valueExpr = Expression.Parameter(attr.ValueType, "retVal");
        var readExpression = Expression.Call(Expression.Constant(serializer), readMethod, valueSpan, valueExpr);
        return Expression.Block([valueExpr], readExpression, valueExpr);
    }
}
