using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
public abstract partial class Attribute<TValueType, TLowLevelType, TSerializer> : IAttribute<TValueType>
    where TValueType : notnull
    where TLowLevelType : notnull
    where TSerializer : IValueSerializer<TLowLevelType>
{
    
    /// <summary>
    /// Constructor used when subclassing, provides a few of the important flags
    /// and the name of the attribute
    /// </summary>
    protected Attribute(
        string ns,
        string name,
        bool isIndexed = false,
        bool noHistory = false,
        Cardinality cardinality = Cardinality.One)
    {
        
        Id = Symbol.Intern(ns, name);
        ShortName = string.Intern($"{ns.Split(".").Last()}/{name}");
        Cardinalty = cardinality;
        IsIndexed = isIndexed;
        NoHistory = noHistory;
    }

    public string ShortName { get; }

    public object FromLowLevelObject(object lowLevel, AttributeResolver resolver)
    {
        return FromLowLevel((TLowLevelType)lowLevel, resolver);
    }

    /// <summary>
    /// Converts a high-level value to a low-level value
    /// </summary>
    public abstract TLowLevelType ToLowLevel(TValueType value);
    
    /// <summary>
    /// Converts a high-level value to a low-level value
    /// </summary>
    public abstract TValueType FromLowLevel(TLowLevelType value, AttributeResolver resolver);

    /// <inheritdoc />
    public ValueTag LowLevelType => TSerializer.ValueTag;

    /// <inheritdoc />
    public Symbol Id { get; }

    /// <inheritdoc />
    public Cardinality Cardinalty { get; init; }

    /// <inheritdoc />
    public bool IsIndexed { get; init; }
    
    /// <inheritdoc />
    public bool IsUnique { get; init; }
    
    /// <summary>
    /// Returns the indexed flags for this attribute
    /// </summary>
    public IndexedFlags IndexedFlags
    {
        get
        {
            if (IsUnique)
                return IndexedFlags.Unique;
            if (IsIndexed)
                return IndexedFlags.Indexed;
            return IndexedFlags.None;
        }
        
    }

    /// <inheritdoc />
    public bool NoHistory { get; init; }

    /// <inheritdoc />
    public virtual bool DeclaredOptional { get; protected init; }

    /// <inheritdoc />
    public Type ValueType => typeof(TValueType);

    /// <inheritdoc />
    public bool IsReference => LowLevelType == ValueTag.Reference;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void AssertTag(ValueTag tag)
    {
        if (tag != LowLevelType)
            throw new InvalidCastException($"Invalid value tag for attribute {Id.Name}");
    }
    
    /// <summary>
    /// Reads the high level value from the given span
    /// </summary>
    public virtual TValueType ReadValue(ReadOnlySpan<byte> span, ValueTag tag, AttributeResolver resolver)
    {
        AssertTag(tag);
        return FromLowLevel(TSerializer.Read(span), resolver);
    }
    
    /// <summary>
    /// Write a datom for this attribute to the given writer
    /// </summary>
    public void Write<TWriter>(EntityId entityId, AttributeCache cache, TValueType value, TxId txId, bool isRetract, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        var prefix = new KeyPrefix(entityId, cache.GetAttributeId(Id), txId, isRetract, LowLevelType);
        var span = writer.GetSpan(KeyPrefix.Size);
        MemoryMarshal.Write(span, prefix);
        writer.Advance(KeyPrefix.Size);
        LowLevelType.Write(ToLowLevel(value), writer);
    }

    /// <summary>
    /// Returns true if the attribute is present on the entity
    /// </summary>
    public bool IsIn(IDb db, EntityId id)
    {
        var attrId = db.AttributeCache.GetAttributeId(Id);
        var index = db[id];
        foreach (var datom in index)
            if (datom.Prefix.A == attrId)
                return true;
        return false;
    }

    /// <summary>
    /// Returns true if the attribute is present on the entity
    /// </summary>
    public bool IsIn<T>(T entity) 
        where T : IHasIdAndEntitySegment
    {
        return entity.EntitySegment.Contains(this);
    }
    
    /// <inheritdoc />
    public override string ToString()
    {
        return Id.ToString();
    }
}
