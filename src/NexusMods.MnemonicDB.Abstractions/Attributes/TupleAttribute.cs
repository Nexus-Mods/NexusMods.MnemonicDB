using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

public class TupleAttribute<T1HighLevel, T1LowLevel, T2HighLevel, T2LowLevel> : ScalarAttribute<(T1HighLevel, T2HighLevel), (T1LowLevel, T2LowLevel)>
{
    private readonly ValueTags _tag1;
    private readonly ValueTags _tag2;

    /// <summary>
    /// Creates a new tuple attribute, the value tags are used to determine the type of each elelement in the tuple.
    /// </summary>
    public TupleAttribute(ValueTags tag1, ValueTags tag2, string ns, string name) : base(ValueTags.Tuple2, ns, name)
    {
        _tag1 = tag1;
        _tag2 = tag2;
        
    }

    /// <inheritdoc />
    public override (T1HighLevel, T2HighLevel) ReadValue(ReadOnlySpan<byte> span, ValueTags tag, RegistryId registryId)
    {
        if (tag != ValueTags.Tuple2)
            throw new ArgumentException($"Expected tag {ValueTags.Tuple2}, but got {tag}");

        var type1 = span[0];
        var type2 = span[1];
        
        if (type1 != (byte)_tag1 || type2 != (byte)_tag2)
            throw new ArgumentException($"Expected tag {_tag1} and {_tag2}, but got {type1} and {type2}");

        var valA = ReadValue<T1LowLevel>(span.SliceFast(2), _tag1, registryId, out var sizeA);
        var valB = ReadValue<T2LowLevel>(span.SliceFast(2 + sizeA), _tag2, registryId, out _);
        
        return FromLowLevel((valA, valB));
    }

    /// <inheritdoc />
    protected virtual (T1HighLevel, T2HighLevel) FromLowLevel((T1LowLevel, T2LowLevel) value)
    {
        throw new NotSupportedException("You must override this method to support low-level conversion.");
    }

    public override void Remap(Func<EntityId, EntityId> remapper, Span<byte> valueSpan)
    {
        if (_tag1 == ValueTags.Reference)
        {
            var entityId = MemoryMarshal.Read<EntityId>(valueSpan.SliceFast(2));
            var newEntityId = remapper(entityId);
            MemoryMarshal.Write(valueSpan.SliceFast(2), newEntityId);
        }
        else if (_tag2 == ValueTags.Reference)
        {
            throw new NotImplementedException();
        }
    }

    /// <inheritdoc />
    public override void WriteValue<TWriter>((T1HighLevel, T2HighLevel) value, TWriter writer)
    {
        var span = writer.GetSpan(2);
        span[0] = (byte)_tag1;
        span[1] = (byte)_tag2;
        writer.Advance(2);
        
        WriteValueLowLevel(value.Item1, _tag1, writer);
        WriteValueLowLevel(value.Item2, _tag2, writer);
    }

    /// <inheritdoc />
    protected override (T1LowLevel, T2LowLevel) ToLowLevel((T1HighLevel, T2HighLevel) value)
    {
        throw new NotSupportedException("This attribute uses custom serialization, and does not support low-level conversion.");
    }
}
