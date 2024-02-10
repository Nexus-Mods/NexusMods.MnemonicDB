using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore;

/// <summary>
/// Tracks all attributes and their respective serializers as well as the DB entity IDs for each
/// attribute
/// </summary>
public class AttributeRegistry
{
    private readonly Dictionary<Type,IValueSerializer> _valueSerializersByNativeType;
    private readonly Dictionary<UInt128,IAttribute> _attributesById;
    private readonly Dictionary<Type,IAttribute> _attributesByType;
    private readonly Dictionary<ulong,DbAttribute> _dbAttributesByEntityId;
    private readonly Dictionary<UInt128,DbAttribute> _dbAttributesByUniqueId;

    /// <summary>
    /// Tracks all attributes and their respective serializers as well as the DB entity IDs for each
    /// attribute
    /// </summary>
    public AttributeRegistry(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    {
        _valueSerializersByNativeType = valueSerializers.ToDictionary(x => x.NativeType);
        _attributesById = attributes.ToDictionary(x => x.Id);
        _attributesByType = attributes.ToDictionary(x => x.GetType());

        _dbAttributesByEntityId = new Dictionary<ulong, DbAttribute>();
        _dbAttributesByUniqueId = new Dictionary<UInt128, DbAttribute>();
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

        return 0;
    }

    public unsafe int CompareValues(ulong attrId, void* aVal, uint aLength, void* bVal, uint bLength)
    {
        var attr = _attributesById[attrId];
        var type = _valueSerializersByNativeType[attr.ValueType];
        return type.Compare(new ReadOnlySpan<byte>(aVal, (int) aLength), new ReadOnlySpan<byte>(bVal, (int) bLength));
    }
}
