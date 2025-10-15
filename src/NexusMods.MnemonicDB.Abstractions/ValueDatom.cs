using System;
using System.Runtime.InteropServices;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Traits;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

public static class ValueDatom
{
    public static IDatomLikeRO Create(KeyPrefix prefix, ReadOnlySpan<byte> valueSpan)
    {
         switch (prefix.ValueTag)
        {
            case ValueTag.UInt8:
                return new ValueDatom<byte>(prefix, prefix.ValueTag.Read<byte>(valueSpan));
            case ValueTag.UInt16:
                return new ValueDatom<ushort>(prefix, prefix.ValueTag.Read<ushort>(valueSpan));
            case ValueTag.UInt32:
                return new ValueDatom<uint>(prefix, prefix.ValueTag.Read<uint>(valueSpan));
            case ValueTag.UInt64:
                return new ValueDatom<ulong>(prefix, prefix.ValueTag.Read<ulong>(valueSpan));
            case ValueTag.UInt128:
                return new ValueDatom<UInt128>(prefix, prefix.ValueTag.Read<UInt128>(valueSpan));
            case ValueTag.Int16:
                return new ValueDatom<short>(prefix, prefix.ValueTag.Read<short>(valueSpan));
            case ValueTag.Int32:
                return new ValueDatom<int>(prefix, prefix.ValueTag.Read<int>(valueSpan));
            case ValueTag.Int64:
                return new ValueDatom<long>(prefix, prefix.ValueTag.Read<long>(valueSpan));
            case ValueTag.Int128:
                return new ValueDatom<Int128>(prefix, prefix.ValueTag.Read<Int128>(valueSpan));
            case ValueTag.Float32:
                return new ValueDatom<float>(prefix, prefix.ValueTag.Read<float>(valueSpan));
            case ValueTag.Float64:
                return new ValueDatom<double>(prefix, prefix.ValueTag.Read<double>(valueSpan));
            case ValueTag.Ascii:
                return new ValueDatom<string>(prefix, prefix.ValueTag.Read<string>(valueSpan));
            case ValueTag.Utf8:
                return new ValueDatom<string>(prefix, prefix.ValueTag.Read<string>(valueSpan));
            case ValueTag.Utf8Insensitive:
                return new ValueDatom<string>(prefix, prefix.ValueTag.Read<string>(valueSpan));
            case ValueTag.Blob:
                return new ValueDatom<Memory<byte>>(prefix, prefix.ValueTag.Read<Memory<byte>>(valueSpan));
            case ValueTag.HashedBlob:
                return new ValueDatom<Memory<byte>>(prefix, prefix.ValueTag.Read<Memory<byte>>(valueSpan));
            case ValueTag.Reference:
                return new ValueDatom<EntityId>(prefix, prefix.ValueTag.Read<EntityId>(valueSpan));
            case ValueTag.Tuple2_UShort_Utf8I:
                return new ValueDatom<(ushort, string)>(prefix, prefix.ValueTag.Read<(ushort, string)>(valueSpan));
            case ValueTag.Tuple3_Ref_UShort_Utf8I:
                return new ValueDatom<(EntityId, ushort, string)>(prefix, prefix.ValueTag.Read<(EntityId, ushort, string)>(valueSpan));
            default:
                throw new ArgumentOutOfRangeException(nameof(prefix), prefix, "Unknown prefix");
        }
    }
    public static IDatomLikeRO Create(ReadOnlySpan<byte> span)
    {
        var prefix = MemoryMarshal.Read<KeyPrefix>(span);
        var rest = span.SliceFast(KeyPrefix.Size);
        return Create(prefix, rest);
    }

    public static IDatomLikeRO Create<TEnum>(in TEnum spanDatomLike) where TEnum : ISpanDatomLikeRO
    {
        var prefix = spanDatomLike.Prefix;
        if (prefix.ValueTag == ValueTag.HashedBlob)
            return Create(prefix, spanDatomLike.ExtraValueSpan);
        return Create(spanDatomLike.Prefix, spanDatomLike.ValueSpan);
    }
}

public class ValueDatom<T> : IDatomLikeRO<T> 
    where T : notnull
{
    private readonly KeyPrefix _prefix;
    private readonly T _value;

    public ValueDatom(KeyPrefix prefix, T value)
    {
        _prefix = prefix;
        _value = value;
    }
    
    public KeyPrefix Prefix => _prefix;
    public object ValueObject => _value;
    public T Value => _value;
}
