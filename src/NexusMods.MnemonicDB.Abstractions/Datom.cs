using System;
using System.Runtime.InteropServices;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Traits;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

public readonly record struct TaggedValue(ValueTag Tag, object Value);

public struct Datom : IComparable<Datom>
{
    public KeyPrefix Prefix { get; }
    public object Value { get; }
    
    public EntityId E => Prefix.E;
    
    public AttributeId A => Prefix.A;
    
    public TxId T => Prefix.T;
    
    public ValueTag Tag => Prefix.ValueTag;
    
    public TaggedValue TaggedValue => new(Prefix.ValueTag, Value);
    
    public bool IsRetract => Prefix.IsRetract;

    public Datom(KeyPrefix prefix, object value)
    {
        Prefix = prefix;
        Value = value;
    }

    public readonly Datom With(IndexType indexType) 
        => new(Prefix with { Index = indexType }, Value);
    
    public readonly Datom With(TxId txId) 
        => new(Prefix with { T = txId }, Value);

    public readonly Datom WithRemaps(Func<EntityId, EntityId> remapFn)
    {
        var newPrefix = Prefix with { E = remapFn(Prefix.E) };
        switch (Prefix.ValueTag)
        {
            case ValueTag.Reference:
                return new Datom(newPrefix, remapFn((EntityId)Value));
            case ValueTag.Tuple3_Ref_UShort_Utf8I:
            {
                var (r, u, s) = (ValueTuple<EntityId, ushort, string>)Value;
                var newValue = (remapFn(r), u, s);
                return new Datom(newPrefix, newValue);
            }
            default:
                return new Datom(newPrefix, Value);
        }
    }

    public static Datom Create<T>(EntityId e, AttributeId attr, ValueTag tag, T value) 
        where T : notnull
    {
        var prefix = new KeyPrefix(e, attr, TxId.Tmp, false, tag);
        return new Datom(prefix, value);
    }
    
    public static Datom Create(EntityId e, AttributeId attr, TaggedValue tValue)
    {
        var prefix = new KeyPrefix(e, attr, TxId.Tmp, false, tValue.Tag);
        return new Datom(prefix, tValue.Value);
    }
    
    public static Datom Create(EntityId e, AttributeId attr, TaggedValue tValue, TxId tx)
    {
        var prefix = new KeyPrefix(e, attr, tx, false, tValue.Tag);
        return new Datom(prefix, tValue.Value);
    }
    
    public static Datom Create(EntityId e, AttributeId attr, TaggedValue tValue, TxId tx, bool isRetract)
    {
        var prefix = new KeyPrefix(e, attr, tx, isRetract, tValue.Tag);
        return new Datom(prefix, tValue.Value);
    }
    
    public static Datom Create<THighLevel, TLowLevel, TSerializer>(EntityId e, Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, AttributeCache attributeCache) 
        where THighLevel : notnull 
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = attributeCache.GetAttributeId(attr.Id);
        var prefix = new KeyPrefix(e, attrId, TxId.Tmp, false, attr.LowLevelType);
        var converted = attr.ToLowLevel(value);
        return new Datom(prefix, converted);
    }
    
    public static Datom Create<THighLevel, TLowLevel, TSerializer>(EntityId e, Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, bool isRetract, AttributeCache attributeCache) 
        where THighLevel : notnull 
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = attributeCache.GetAttributeId(attr.Id);
        var prefix = new KeyPrefix(e, attrId, TxId.Tmp, isRetract, attr.LowLevelType);
        var converted = attr.ToLowLevel(value);
        return new Datom(prefix, converted);
    }
    
    public static Datom Create(KeyPrefix prefix, Datom src)
    {
        return new Datom(prefix, src.Value);
    }
    
    public static Datom Create(KeyPrefix prefix, ReadOnlySpan<byte> valueSpan)
    {
        switch (prefix.ValueTag)
        {
            case ValueTag.Null:
                return new Datom(prefix, Null.Instance);
            case ValueTag.UInt8:
                return new Datom(prefix, prefix.ValueTag.Read<byte>(valueSpan));
            case ValueTag.UInt16:
                return new Datom(prefix, prefix.ValueTag.Read<ushort>(valueSpan));
            case ValueTag.UInt32:
                return new Datom(prefix, prefix.ValueTag.Read<uint>(valueSpan));
            case ValueTag.UInt64:
                return new Datom(prefix, prefix.ValueTag.Read<ulong>(valueSpan));
            case ValueTag.UInt128:
                return new Datom(prefix, prefix.ValueTag.Read<UInt128>(valueSpan));
            case ValueTag.Int16:
                return new Datom(prefix, prefix.ValueTag.Read<short>(valueSpan));
            case ValueTag.Int32:
                return new Datom(prefix, prefix.ValueTag.Read<int>(valueSpan));
            case ValueTag.Int64:
                return new Datom(prefix, prefix.ValueTag.Read<long>(valueSpan));
            case ValueTag.Int128:
                return new Datom(prefix, prefix.ValueTag.Read<Int128>(valueSpan));
            case ValueTag.Float32:
                return new Datom(prefix, prefix.ValueTag.Read<float>(valueSpan));
            case ValueTag.Float64:
                return new Datom(prefix, prefix.ValueTag.Read<double>(valueSpan));
            case ValueTag.Ascii:
                return new Datom(prefix, prefix.ValueTag.Read<string>(valueSpan));
            case ValueTag.Utf8:
                return new Datom(prefix, prefix.ValueTag.Read<string>(valueSpan));
            case ValueTag.Utf8Insensitive:
                return new Datom(prefix, prefix.ValueTag.Read<string>(valueSpan));
            case ValueTag.Blob:
                return new Datom(prefix, prefix.ValueTag.Read<Memory<byte>>(valueSpan));
            case ValueTag.HashedBlob:
                return new Datom(prefix, prefix.ValueTag.Read<Memory<byte>>(valueSpan));
            case ValueTag.Reference:
                return new Datom(prefix, prefix.ValueTag.Read<EntityId>(valueSpan));
            case ValueTag.Tuple2_UShort_Utf8I:
                return new Datom(prefix, prefix.ValueTag.Read<(ushort, string)>(valueSpan));
            case ValueTag.Tuple3_Ref_UShort_Utf8I:
                return new Datom(prefix, prefix.ValueTag.Read<(EntityId, ushort, string)>(valueSpan));
            default:
                throw new ArgumentOutOfRangeException(nameof(prefix), prefix, $"Unknown prefix tag {prefix.ValueTag}");
        }
    }

    public static Datom Create<TEnum>(in TEnum spanDatomLike) 
        where TEnum : ISpanDatomLikeRO, allows ref struct
    {
        var prefix = spanDatomLike.Prefix;
        if (prefix.ValueTag == ValueTag.HashedBlob)
        {
            var extraData = spanDatomLike.ExtraValueSpan;
            var blob = GC.AllocateArray<byte>(extraData.Length + sizeof(ulong));
            extraData.CopyTo(blob.AsSpan().SliceFast(sizeof(ulong)));
            spanDatomLike.ValueSpan.CopyTo(blob.AsSpan());
            return new(prefix, blob.AsMemory());
        }

        return Create(spanDatomLike.Prefix, spanDatomLike.ValueSpan);
    }

    public readonly Datom WithRetract(bool b = true)
    {
        return new Datom(Prefix with { IsRetract = b }, Value);   
    }
    
    /// <summary>
    /// A datom with the minimum values for each property
    /// </summary>
    public static readonly Datom Min = new(new KeyPrefix(EntityId.MinValueNoPartition, AttributeId.Min, TxId.MinValue, false, ValueTag.Null), Null.Instance);
    
    /// <summary>
    /// A datom with the maximum values for each property
    /// </summary>
    public static readonly Datom Max = new(new KeyPrefix(EntityId.MaxValueNoPartition, AttributeId.Max, TxId.MaxValue, true, ValueTag.Null), Null.Instance);

    /// <inheritdoc />
    public int CompareTo(Datom other)
    {
        return GlobalComparer.Compare(this, other);
    }
}
