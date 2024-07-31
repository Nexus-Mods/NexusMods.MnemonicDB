using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Storage;

/// <summary>
///     Tracks all attributes and their respective serializers as well as the DB entity IDs for each
///     attribute
/// </summary>
public class AttributeRegistry : IAttributeRegistry, IDisposable
{
    
    private static RegistryId GetRegistryId(AttributeRegistry registry)
    {
        var registries = AttributeRegistryRegistry.Registries;
        lock (registries)
        {
            for (var i = 0; i < registries.Length; i++)
            {
                if (registries[i] != null) continue;

                registries[i] = registry;
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
    public AttributeRegistry(IServiceProvider provider, IEnumerable<IAttribute> attributes)
    {
        Id = GetRegistryId(this);
        ServiceProvider = provider;
        _attributesBySymbol = attributes.ToDictionary(a => a.Id);
    }

    /// <inheritdoc />
    public RegistryId Id { get; }

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    public IReadDatom Resolve(in KeyPrefix prefix, ReadOnlySpan<byte> valueSpan)
    {
        var attr = _attributes[prefix.A.Value];
        return attr.Resolve(prefix, valueSpan, Id);
    }

    /// <inheritdoc />
    public void Populate(IEnumerable<DbAttribute> attributes)
    {
        foreach (var dbAttribute in attributes)
        {
            // Do a try/get here because an attribute may not exist in code that exists in the database
            if (!_attributesBySymbol.TryGetValue(dbAttribute.UniqueId, out var instance))
            {
                instance = UnknownAttribute.Create(dbAttribute);
                _attributesBySymbol[dbAttribute.UniqueId] = instance;
            }

            instance.SetDbId(Id, dbAttribute.AttrEntityId);
            _attributes[dbAttribute.AttrEntityId.Value] = instance;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAttribute GetAttribute(AttributeId attributeId)
    {
        var attr = _attributes[attributeId.Value];
        if (attr == null)
            throw new InvalidOperationException($"No attribute found for attribute ID {attributeId}, did you forget to register it?");
        return attr;
    }

    public void Dispose()
    {
        AttributeRegistryRegistry.Registries[Id.Value] = null!;
    }
}
