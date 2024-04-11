﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage;

/// <summary>
///     Tracks all attributes and their respective serializers as well as the DB entity IDs for each
///     attribute
/// </summary>
public class AttributeRegistry : IAttributeRegistry, IDisposable
{
    private static readonly AttributeRegistry?[] _registries = new AttributeRegistry[8];

    private static RegistryId GetRegistryId(AttributeRegistry registry)
    {
        lock (_registries)
        {
            for (var i = 0; i < _registries.Length; i++)
            {
                if (_registries[i] != null) continue;

                _registries[i] = registry;
                return RegistryId.From((byte)i);
            }
        }

        throw new IndexOutOfRangeException("Too many attribute registries created");
    }

    private readonly Dictionary<Symbol, IAttribute> _attributesBySymbol = new ();
    private AttributeArray _attributes;

    /// <summary>
    /// We will likely only ever have one of these per program, we're overallocating this
    /// to sizeof(ref) * 64K but that's probably fine in exchange for the speedup we get for
    /// this
    /// </summary>
    [InlineArray(ushort.MaxValue)]
    private struct AttributeArray
    {
        private IAttribute _attribute;
    }


    /// <summary>
    ///     Tracks all attributes and their respective serializers as well as the DB entity IDs for each
    ///     attribute
    /// </summary>
    public AttributeRegistry(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    {
        Id = GetRegistryId(this);

        var serializers = valueSerializers.ToDictionary(s => s.NativeType);

        BuiltInAttributes.UniqueId.SetDbId(Id, BuiltInAttributes.UniqueIdEntityId);
        BuiltInAttributes.ValueSerializerId.SetDbId(Id, BuiltInAttributes.ValueSerializerIdEntityId);

        foreach (var attribute in attributes)
        {
            attribute.SetSerializer(serializers[attribute.ValueType]);
            _attributesBySymbol[attribute.Id] = attribute;
        }
    }

    /// <inheritdoc />
    public RegistryId Id { get; }

    public IReadDatom Resolve(ReadOnlySpan<byte> datom)
    {
        var prefix = MemoryMarshal.Read<KeyPrefix>(datom);

        var attr = _attributes[prefix.A.Value];
        unsafe
        {
            return attr.Resolve(prefix,datom.SliceFast(sizeof(KeyPrefix)));
        }
    }

    public TVal Resolve<TVal>(ReadOnlySpan<byte> datom)
    {
        var prefix = MemoryMarshal.Read<KeyPrefix>(datom);
        var attr = _attributes[prefix.A.Value];

        return ((IValueSerializer<TVal>)attr.Serializer).Read(prefix, datom.SliceFast(KeyPrefix.Size));
    }

    public void Populate(DbAttribute[] attributes)
    {
        foreach (var dbAttribute in attributes)
        {
            var instance = _attributesBySymbol[dbAttribute.UniqueId];
            instance.SetDbId(Id, dbAttribute.AttrEntityId);
            _attributes[dbAttribute.AttrEntityId.Value] = instance;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAttribute GetAttribute(AttributeId attributeId)
    {
        var attr = _attributes[attributeId.Value];
        if (attr == null)
            throw new InvalidOperationException($"No attribute found for attribute ID {attributeId}");
        return attr;
    }

    public void Dispose()
    {
        _registries[Id.Value] = null!;
    }
}
