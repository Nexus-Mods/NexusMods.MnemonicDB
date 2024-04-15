using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
/// <typeparam name="TValueType"></typeparam>
public abstract class Attribute<TValueType, TLowLevelType> : IAttribute
{
    protected RegistryId.InlineCache Cache;

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
    protected virtual TValueType FromLowLevel(byte value, ValueTags tags)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(ushort value, ValueTags tags)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }


    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(string value, ValueTags tags)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }


    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(ulong value, ValueTags tags)
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
    public void SetDbId(RegistryId id, AttributeId attributeId)
    {
        Cache[id.Value] = attributeId;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AttributeId GetDbId(RegistryId id)
    {
        return Cache[id.Value];
    }

    /// <inheritdoc />
    public Type ValueType => typeof(TValueType);

    /// <inheritdoc />
    public bool IsReference => typeof(TValueType) == typeof(EntityId);

    /// <inheritdoc />
    public IReadDatom Resolve(EntityId entityId, AttributeId attributeId, ReadOnlySpan<byte> value, TxId tx,
        bool isRetract)
    {
        return new ReadDatom
        {
            E = entityId,
            A = this,
            V = ReadValue(value),
            T = tx,
            IsRetract = isRetract
        };
    }

    private void ThrowKeyNotFoundException(EntityId id)
    {
        throw new KeyNotFoundException($"Attribute {Id} not found on entity {id}");
    }



    /// <inheritdoc />
    public void Add(IEntity entity, TValueType value)
    {
        entity.Tx!.Add(entity.Id, this, value);
    }



    private void WriteValueLowLevel<TWriter>(TLowLevelType value, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        switch (value)
        {
            case byte b:
                WriteUnmanaged(b, writer);
                break;
            case short s:
                WriteUnmanaged(s, writer);
                break;
            default:
                throw new NotSupportedException("Unsupported low-level type" + value);
        }
    }

    public TValueType ReadValue(ReadOnlySpan<byte> span)
    {
        var tag = (ValueTags)span[0];
        return LowLevelType switch
        {
            ValueTags.UInt8 => FromLowLevel(ReadUnmanaged<byte>(span), tag),
            ValueTags.UInt16 => FromLowLevel(ReadUnmanaged<ushort>(span), tag),
            _ => throw new NotSupportedException("Unsupported low-level type" + LowLevelType)
        };
    }

    private unsafe void WriteUnmanaged<TWriter, TValue>(TValue value, TWriter writer)
        where TWriter : IBufferWriter<byte>
        where TValue : unmanaged
    {
        var span = writer.GetSpan(sizeof(TValue) + 1);
        span[0] = (byte) LowLevelType;
        MemoryMarshal.Write(span, value);
        writer.Advance(sizeof(TValue) + 1);
    }

    private TValue ReadUnmanaged<TValue>(ReadOnlySpan<byte> span)
        where TValue : unmanaged
    {
        return MemoryMarshal.Read<TValue>(span);
    }

    /// <summary>
    /// Write a datom for this attribute to the given writer
    /// </summary>
    public void Write<TWriter>(EntityId entityId, RegistryId registryId, TValueType value, TxId txId, bool isRetract, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        var prefix = new KeyPrefix().Set(entityId, GetDbId(registryId), txId, isRetract);
        var span = writer.GetSpan(KeyPrefix.Size);
        MemoryMarshal.Write(span, prefix);
        writer.Advance(KeyPrefix.Size);
        WriteValueLowLevel(ToLowLevel(value), writer);
    }


    /// <summary>
    /// Write a datom for this attribute to the given writer
    /// </summary>
    public void WriteValue<TWriter>(TValueType value, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        WriteValueLowLevel(ToLowLevel(value), writer);
    }

    /// <summary>
    ///     Typed datom for this attribute
    /// </summary>
    public readonly record struct ReadDatom : IReadDatom
    {
        private readonly ulong _tx;

        /// <summary>
        ///     The value for this datom
        /// </summary>
        public required TValueType V { get; init; }

        /// <summary>
        ///     The attribute for this datom
        /// </summary>
        public required IAttribute A { get; init; }

        /// <summary>
        ///     The entity id for this datom
        /// </summary>
        public required EntityId E { get; init; }

        /// <summary>
        ///     The transaction id for this datom
        /// </summary>
        public TxId T
        {
            get => TxId.From(_tx >> 1);
            init => _tx = (_tx & 1) | (value.Value << 1);
        }

        /// <inheritdoc />
        public bool IsRetract
        {
            get => (_tx & 1) == 1;
            init => _tx = (_tx & ~1UL) | (value ? 1UL : 0);
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
    }

}
