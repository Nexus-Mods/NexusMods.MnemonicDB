using System;
using System.Runtime.InteropServices;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Traits;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

public readonly record struct TaggedValue(ValueTag Tag, object Value);

public struct ValueDatom : IDatomLikeRO
{
    public KeyPrefix Prefix { get; }
    public object Value { get; }
    
    public TaggedValue TaggedValue => new(Prefix.ValueTag, Value);

    public ValueDatom(KeyPrefix prefix, object value)
    {
        Prefix = prefix;
        Value = value;
    }
    
    public static ValueDatom Create<T>(EntityId e, AttributeId attr, ValueTag tag, T value) 
        where T : notnull
    {
        var prefix = new KeyPrefix(e, attr, TxId.Tmp, false, tag);
        return new ValueDatom(prefix, value);
    }
    
    public static ValueDatom Create<THighLevel, TLowLevel, TSerializer>(EntityId e, Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, AttributeCache attributeCache) 
        where THighLevel : notnull 
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = attributeCache.GetAttributeId(attr.Id);
        var prefix = new KeyPrefix(e, attrId, TxId.Tmp, false, attr.LowLevelType);
        var converted = attr.ToLowLevel(value);
        return new ValueDatom(prefix, converted);
    }
    
    public static ValueDatom Create<THighLevel, TLowLevel, TSerializer>(EntityId e, Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, bool isRetract, AttributeCache attributeCache) 
        where THighLevel : notnull 
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = attributeCache.GetAttributeId(attr.Id);
        var prefix = new KeyPrefix(e, attrId, TxId.Tmp, isRetract, attr.LowLevelType);
        var converted = attr.ToLowLevel(value);
        return new ValueDatom(prefix, converted);
    }
    
    public static ValueDatom Create(KeyPrefix prefix, IDatomLikeRO valueSrc)
    {
        return new ValueDatom(prefix, valueSrc.Value);
    }
    
    public static ValueDatom Create(KeyPrefix prefix, ReadOnlySpan<byte> valueSpan)
    {
        switch (prefix.ValueTag)
        {
            case ValueTag.UInt8:
                return new ValueDatom(prefix, prefix.ValueTag.Read<byte>(valueSpan));
            case ValueTag.UInt16:
                return new ValueDatom(prefix, prefix.ValueTag.Read<ushort>(valueSpan));
            case ValueTag.UInt32:
                return new ValueDatom(prefix, prefix.ValueTag.Read<uint>(valueSpan));
            case ValueTag.UInt64:
                return new ValueDatom(prefix, prefix.ValueTag.Read<ulong>(valueSpan));
            case ValueTag.UInt128:
                return new ValueDatom(prefix, prefix.ValueTag.Read<UInt128>(valueSpan));
            case ValueTag.Int16:
                return new ValueDatom(prefix, prefix.ValueTag.Read<short>(valueSpan));
            case ValueTag.Int32:
                return new ValueDatom(prefix, prefix.ValueTag.Read<int>(valueSpan));
            case ValueTag.Int64:
                return new ValueDatom(prefix, prefix.ValueTag.Read<long>(valueSpan));
            case ValueTag.Int128:
                return new ValueDatom(prefix, prefix.ValueTag.Read<Int128>(valueSpan));
            case ValueTag.Float32:
                return new ValueDatom(prefix, prefix.ValueTag.Read<float>(valueSpan));
            case ValueTag.Float64:
                return new ValueDatom(prefix, prefix.ValueTag.Read<double>(valueSpan));
            case ValueTag.Ascii:
                return new ValueDatom(prefix, prefix.ValueTag.Read<string>(valueSpan));
            case ValueTag.Utf8:
                return new ValueDatom(prefix, prefix.ValueTag.Read<string>(valueSpan));
            case ValueTag.Utf8Insensitive:
                return new ValueDatom(prefix, prefix.ValueTag.Read<string>(valueSpan));
            case ValueTag.Blob:
                return new ValueDatom(prefix, prefix.ValueTag.Read<Memory<byte>>(valueSpan));
            case ValueTag.HashedBlob:
                return new ValueDatom(prefix, prefix.ValueTag.Read<Memory<byte>>(valueSpan));
            case ValueTag.Reference:
                return new ValueDatom(prefix, prefix.ValueTag.Read<EntityId>(valueSpan));
            case ValueTag.Tuple2_UShort_Utf8I:
                return new ValueDatom(prefix, prefix.ValueTag.Read<(ushort, string)>(valueSpan));
            case ValueTag.Tuple3_Ref_UShort_Utf8I:
                return new ValueDatom(prefix, prefix.ValueTag.Read<(EntityId, ushort, string)>(valueSpan));
            default:
                throw new ArgumentOutOfRangeException(nameof(prefix), prefix, "Unknown prefix");
        }
    }
    public static ValueDatom Create(ReadOnlySpan<byte> span)
    {
        var prefix = MemoryMarshal.Read<KeyPrefix>(span);
        var rest = span.SliceFast(KeyPrefix.Size);
        return Create(prefix, rest);
    }

    public static ValueDatom Create<TEnum>(in TEnum spanDatomLike) where TEnum : ISpanDatomLikeRO
    {
        var prefix = spanDatomLike.Prefix;
        if (prefix.ValueTag == ValueTag.HashedBlob)
            return Create(prefix, spanDatomLike.ExtraValueSpan);
        return Create(spanDatomLike.Prefix, spanDatomLike.ValueSpan);
    }
}
