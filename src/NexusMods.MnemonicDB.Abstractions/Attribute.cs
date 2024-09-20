using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Exceptions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
/// <typeparam name="TValueType"></typeparam>
public abstract partial class Attribute<TValueType, TLowLevelType> : IAttribute<TValueType>
{
    private const int MaxStackAlloc = 128;
    private static Encoding AsciiEncoding = Encoding.ASCII;

    private static Encoding Utf8Encoding = Encoding.UTF8;

    protected Attribute(
        ValueTags lowLevelType,
        string ns,
        string name,
        bool isIndexed = false,
        bool noHistory = false,
        Cardinality cardinality = Cardinality.One)
    {
        LowLevelType = lowLevelType;
        Id = Symbol.Intern(ns, name);
        Cardinalty = cardinality;
        IsIndexed = isIndexed;
        NoHistory = noHistory;
    }

    /// <summary>
    /// Converts a high-level value to a low-level value
    /// </summary>
    protected abstract TLowLevelType ToLowLevel(TValueType value);

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(byte value, ValueTags tags, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(ushort value, ValueTags tags, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(uint value, ValueTags tags, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }


    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(string value, ValueTags tag, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(ReadOnlySpan<byte> value, ValueTags tag, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + tag + " on attribute " + Id);
    }


    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(ulong value, ValueTags tags, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }


    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(UInt128 value, ValueTags tags, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(short value, ValueTags tags, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(int value, ValueTags tags, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(long value, ValueTags tags, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(Int128 value, ValueTags tags, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(float value, ValueTags tags, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(double value, ValueTags tags, AttributeResolver resolver)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <inheritdoc />
    public ValueTags LowLevelType { get; }

    /// <inheritdoc />
    public Symbol Id { get; }

    /// <inheritdoc />
    public Cardinality Cardinalty { get; init; }

    /// <inheritdoc />
    public bool IsIndexed { get; init; }

    /// <inheritdoc />
    public bool NoHistory { get; init; }

    /// <inheritdoc />
    public virtual bool DeclaredOptional { get; protected init; }

    /// <inheritdoc />
    public Type ValueType => typeof(TValueType);

    /// <inheritdoc />
    public bool IsReference => LowLevelType == ValueTags.Reference;

    /// <inheritdoc />
    IReadDatom IAttribute.Resolve(in KeyPrefix prefix, ReadOnlySpan<byte> valueSpan, AttributeResolver resolver)
    {
        return new ReadDatom(in prefix, ReadValue(valueSpan, prefix.ValueTag, resolver), this);
    }
    
    /// <summary>
    /// Resolves the value from the given value span into a high-level ReadDatom
    /// </summary>
    public ReadDatom Resolve(in KeyPrefix prefix, ReadOnlySpan<byte> valueSpan, AttributeResolver resolver)
    {
        return new ReadDatom(in prefix, ReadValue(valueSpan, prefix.ValueTag, resolver), this);
    }

    /// <summary>
    /// Resolves the low-level Datom into a high-level ReadDatom
    /// </summary>
    public ReadDatom Resolve(in Datom datom, AttributeResolver resolver)
    {
        var prefix = datom.Prefix;
        return new ReadDatom(in prefix, ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver), this);
    }

    /// <summary>
    /// Returns true if the attribute is present on the entity
    /// </summary>
    public bool IsIn(IDb db, EntityId id)
    {
        var index = db.Get(id);
        return index.Contains(this);
    }

    /// <summary>
    /// Returns true if the attribute is present on the entity
    /// </summary>
    public bool IsIn<T>(T entity)
    where T : IHasIdAndIndexSegment
    {
        return entity.IndexSegment.Contains(this);
    }

    /// <inheritdoc />
    public virtual void Remap(Func<EntityId, EntityId> remapper, Span<byte> valueSpan)
    {
        if (LowLevelType == ValueTags.Reference)
        {
            var id = MemoryMarshal.Read<EntityId>(valueSpan);
            var newId = remapper(id);
            MemoryMarshal.Write(valueSpan, newId);
        }
    }

    private void ThrowKeyNotFoundException(EntityId id)
    {
        throw new KeyNotFoundException($"Attribute {Id} not found on entity {id}");
    }

    /// <summary>
    /// Adds a datom to the active transaction for this entity that adds the given value to this attribute
    /// </summary>
    public void Add(IAttachedEntity entity, TValueType value)
    {
        entity.Transaction.Add(entity.Id, this, value);
    }


    /// <inheritdoc />
    public void Add(ITransaction tx, EntityId entityId, TValueType value, bool isRetract = false)
    {
        tx.Add(entityId, this, value, isRetract);
    }

    /// <inheritdoc />
    public void Add(ITransaction tx, EntityId entityId, object value, bool isRetract)
    {
        tx.Add(entityId, this, (TValueType)value, isRetract);
    }

    /// <summary>
    /// Adds a datom to the active transaction for this entity that retracts the given value from this attribute
    /// </summary>
    public void Retract(IAttachedEntity entity, TValueType value)
    {
        entity.Transaction.Add(entity.Id, this, value, isRetract:true);
    }


    /// <inheritdoc />
    public override string ToString()
    {
        return Id.ToString();
    }
    
    /// <summary>
    ///     Typed datom for this attribute
    /// </summary>
    public readonly record struct ReadDatom : IReadDatom
    {
        public readonly KeyPrefix Prefix;

        /// <summary>
        ///     Typed datom for this attribute
        /// </summary>
        public ReadDatom(in KeyPrefix prefix, TValueType v, Attribute<TValueType, TLowLevelType> a)
        {
            Prefix = prefix;
            TypedAttribute = a;
            V = v;
        }

        /// <summary>
        /// The typed attribute for this datom
        /// </summary>
        public readonly Attribute<TValueType, TLowLevelType> TypedAttribute;
        
        /// <summary>
        /// The abstract attribute for this datom
        /// </summary>
        public IAttribute A => TypedAttribute;

        /// <summary>
        ///     The value for this datom
        /// </summary>
        public readonly TValueType V;
        
        /// <summary>
        ///     The entity id for this datom
        /// </summary>
        public EntityId E => Prefix.E;

        /// <summary>
        ///     The transaction id for this datom
        /// </summary>
        public TxId T => Prefix.T;

        /// <inheritdoc />
        public bool IsRetract => Prefix.IsRetract;
        
        /// <inheritdoc />
        public void Retract(ITransaction tx)
        {
            tx.Add(E, (Attribute<TValueType, TLowLevelType>)A, V, true);
        }

        /// <inheritdoc />
        public object ObjectValue => V!;

        /// <inheritdoc />
        public Type ValueType => typeof(TValueType);
        
        /// <inheritdoc />
        public override string ToString()
        {
            return $"({(IsRetract ? "-" : "+")}, {E.Value:x}, {A.Id.Name}, {V}, {T.Value:x})";
        }

        /// <inheritdoc />
        public bool EqualsByValue(IReadDatom other)
        {
            if (other is not ReadDatom o)
                return false;
            return A == o.A && E == o.E && V!.Equals(o.V);
        }

        /// <inheritdoc />
        public int HashCodeByValue()
        {
            return HashCode.Combine(A, E, V);
        }
    }
}
