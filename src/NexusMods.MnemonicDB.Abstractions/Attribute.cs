using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Exceptions;
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
    private const int MaxStackAlloc = 128;
    private static Encoding AsciiEncoding = Encoding.ASCII;

    private static Encoding Utf8Encoding = Encoding.UTF8;


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
    protected virtual TValueType FromLowLevel(string value, ValueTags tag)
    {
        throw new NotSupportedException("Unsupported low-level type " + tag + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(ReadOnlySpan<byte> value, ValueTags tag)
    {
        throw new NotSupportedException("Unsupported low-level type " + tag + " on attribute " + Id);
    }


    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(ulong value, ValueTags tags)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }


    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(UInt128 value, ValueTags tags)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(short value, ValueTags tags)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(int value, ValueTags tags)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(long value, ValueTags tags)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(Int128 value, ValueTags tags)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(float value, ValueTags tags)
    {
        throw new NotSupportedException("Unsupported low-level type " + value + " on attribute " + Id);
    }

    /// <summary>
    /// Converts a low-level value to a high-level value
    /// </summary>
    protected virtual TValueType FromLowLevel(double value, ValueTags tags)
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
    public bool IsReference => LowLevelType == ValueTags.Reference;

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

    public bool IsIn(IDb db, EntityId id)
    {
        var index = db.GetSegment(id);
        return index.Contains(this);
    }

    private void ThrowKeyNotFoundException(EntityId id)
    {
        throw new KeyNotFoundException($"Attribute {Id} not found on entity {id}");
    }



    /// <inheritdoc />
    public void Add(IEntityWithTx entity, TValueType value)
    {
        entity.Tx!.Add(entity.Id, this, value);
    }



    private void WriteValueLowLevel<TWriter>(TLowLevelType value, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        switch (value)
        {
            case byte val:
                WriteUnmanaged(val, writer);
                break;
            case ushort val:
                WriteUnmanaged(val, writer);
                break;
            case uint val:
                WriteUnmanaged(val, writer);
                break;
            case ulong val:
                WriteUnmanaged(val, writer);
                break;
            case UInt128 val:
                WriteUnmanaged(val, writer);
                break;
            case short val:
                WriteUnmanaged(val, writer);
                break;
            case int val:
                WriteUnmanaged(val, writer);
                break;
            case long val:
                WriteUnmanaged(val, writer);
                break;
            case Int128 val:
                WriteUnmanaged(val, writer);
                break;
            case float val:
                WriteUnmanaged(val, writer);
                break;
            case double val:
                WriteUnmanaged(val, writer);
                break;
            case string s when LowLevelType == ValueTags.Ascii:
                WriteAscii(s, writer);
                break;
            case string s when LowLevelType == ValueTags.Utf8:
                WriteUtf8(s, writer);
                break;
            case string s when LowLevelType == ValueTags.Utf8Insensitive:
                WriteUtf8(s, writer);
                break;
            default:
                throw new UnsupportedLowLevelWriteType<TLowLevelType>(value);
        }
    }

    private void WriteAscii<TWriter>(string s, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var size = s.Length;
        var span = writer.GetSpan(size + 1);
        span[0] = (byte)LowLevelType;
        AsciiEncoding.GetBytes(s, span.SliceFast(1));
        writer.Advance(size + 1);
    }

    private void WriteUtf8<TWriter>(string s, TWriter writer) where TWriter : IBufferWriter<byte>
    {
        var size = Utf8Encoding.GetByteCount(s);
        var span = writer.GetSpan(size + 1);
        span[0] = (byte)LowLevelType;
        Utf8Encoding.GetBytes(s, span.SliceFast(1));
        writer.Advance(size + 1);
    }

    public TValueType ReadValue(ReadOnlySpan<byte> span)
    {
        var tag = (ValueTags)span[0];
        var rest = span.SliceFast(1);
        Debug.Assert(tag == LowLevelType, "Tag mismatch");
        return tag switch
        {
            ValueTags.UInt8 => FromLowLevel(ReadUnmanaged<byte>(rest), tag),
            ValueTags.UInt16 => FromLowLevel(ReadUnmanaged<ushort>(rest), tag),
            ValueTags.UInt64 => FromLowLevel(ReadUnmanaged<ulong>(rest), tag),
            ValueTags.UInt128 => FromLowLevel(ReadUnmanaged<UInt128>(rest), tag),
            ValueTags.Int16 => FromLowLevel(ReadUnmanaged<short>(rest), tag),
            ValueTags.Int32 => FromLowLevel(ReadUnmanaged<int>(rest), tag),
            ValueTags.Int64 => FromLowLevel(ReadUnmanaged<long>(rest), tag),
            ValueTags.Int128 => FromLowLevel(ReadUnmanaged<Int128>(rest), tag),
            ValueTags.Float32 => FromLowLevel(ReadUnmanaged<float>(rest), tag),
            ValueTags.Float64 => FromLowLevel(ReadUnmanaged<double>(rest), tag),
            ValueTags.Reference => FromLowLevel(ReadUnmanaged<ulong>(rest), tag),
            ValueTags.Ascii => FromLowLevel(ReadAscii(rest), tag),
            ValueTags.Utf8 => FromLowLevel(ReadUtf8(rest), tag),
            ValueTags.Utf8Insensitive => FromLowLevel(ReadUtf8(rest), tag),
            ValueTags.Blob => FromLowLevel(rest, tag),
            ValueTags.HashedBlob => FromLowLevel(rest.SliceFast(sizeof(ulong)), tag),
            _ => throw new UnsupportedLowLevelReadType(tag)
        };
    }

    private string ReadUtf8(ReadOnlySpan<byte> span)
    {
        return Utf8Encoding.GetString(span);
    }

    private string ReadAscii(ReadOnlySpan<byte> span)
    {
        return AsciiEncoding.GetString(span);
    }

    private unsafe void WriteUnmanaged<TWriter, TValue>(TValue value, TWriter writer)
        where TWriter : IBufferWriter<byte>
        where TValue : unmanaged
    {
        var span = writer.GetSpan(sizeof(TValue) + 1);
        span[0] = (byte) LowLevelType;
        MemoryMarshal.Write(span.SliceFast(1), value);
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
    public virtual void Write<TWriter>(EntityId entityId, RegistryId registryId, TValueType value, TxId txId, bool isRetract, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        Debug.Assert(LowLevelType != ValueTags.Blob, "Blobs should overwrite this method and throw when ToLowLevel is called");
        var prefix = new KeyPrefix().Set(entityId, GetDbId(registryId), txId, isRetract);
        var span = writer.GetSpan(KeyPrefix.Size);
        MemoryMarshal.Write(span, prefix);
        writer.Advance(KeyPrefix.Size);
        WriteValueLowLevel(ToLowLevel(value), writer);
    }

    /// <summary>
    /// Write the key prefix for this attribute to the given writer
    /// </summary>
    protected void WritePrefix<TWriter>(EntityId entityId, RegistryId registryId, TxId txId, bool isRetract, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        var prefix = new KeyPrefix().Set(entityId, GetDbId(registryId), txId, isRetract);
        var span = writer.GetSpan(KeyPrefix.Size);
        MemoryMarshal.Write(span, prefix);
        writer.Advance(KeyPrefix.Size);
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
