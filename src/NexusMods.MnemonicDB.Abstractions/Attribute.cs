using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.Cascade;
using NexusMods.Cascade.Flows;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
public abstract partial class Attribute<TValueType, TLowLevelType, TSerializer> : IAttribute<TValueType>, IAttributeFlow<TValueType>
    where TValueType : notnull
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
        ShortName = $"{ns.Split(".").Last()}/{name}";
        Cardinalty = cardinality;
        IsIndexed = isIndexed;
        NoHistory = noHistory;
        StepFn = AttributeStepFn;
        Upstream = [Cascade.Query.Db];
        DebugInfo = new DebugInfo
        {
            Name = "MnemonicDB Attr",
            Expression = Id.ToString()
        };
        AttributeWithTxIdFlow = new UnaryFlow<IDb,(EntityId Id, TValueType Value, EntityId TxId)>
        {
            DebugInfo = new()
            {
                Name = "MnemonicDB AttrWithTx",
                Expression = Id!.ToString()
            },
            Upstream = [Cascade.Query.Db],
            StepFn = AttributeWithTxIdStepFn,
        };
        
        AttributeHistoryFlow = new UnaryFlow<IDb,(EntityId Id, TValueType Value, EntityId TxId)>
        {
            DebugInfo = new()
            {
                Name = "MnemonicDB Attribute History",
                Expression = Id!.ToString()
            },
            Upstream = [Cascade.Query.Db],
            StepFn = AttributeHistoryStepFn,
        };
        
    }

    public string ShortName { get; }

    /// <summary>
    /// Converts a high-level value to a low-level value
    /// </summary>
    protected abstract TLowLevelType ToLowLevel(TValueType value);
    
    /// <summary>
    /// Converts a high-level value to a low-level value
    /// </summary>
    protected abstract TValueType FromLowLevel(TLowLevelType value, AttributeResolver resolver);

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
        var index = db.Get(id);
        return index.Contains(this);
    }

    /// <summary>
    /// Returns true if the attribute is present on the entity
    /// </summary>
    public bool IsIn<T>(T entity) 
        where T : IHasIdAndEntitySegment
    {
        return entity.EntitySegment.Contains(this);
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
        /// <summary>
        /// The key prefix for this datom, contains the E, A, T, IsRetract and ValueTag values for this datom
        /// </summary>
        public readonly KeyPrefix Prefix;

        /// <summary>
        ///     Typed datom for this attribute
        /// </summary>
        public ReadDatom(in KeyPrefix prefix, TValueType v, Attribute<TValueType, TLowLevelType, TSerializer> a)
        {
            Prefix = prefix;
            TypedAttribute = a;
            V = v;
        }

        /// <summary>
        /// The typed attribute for this datom
        /// </summary>
        public readonly Attribute<TValueType, TLowLevelType, TSerializer> TypedAttribute;
        
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
            tx.Add(E, (Attribute<TValueType, TLowLevelType, TSerializer>)A, V, true);
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
