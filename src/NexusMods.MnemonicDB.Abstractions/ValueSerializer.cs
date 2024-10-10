using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

public partial class Attribute<TValueType, TLowLevelType>
{
    /// <summary>
    /// Reads the high level value from the given span
    /// </summary>
    public TValueType ReadValue(ReadOnlySpan<byte> span, ValueTag tag, AttributeResolver resolver)
    {
        return FromLowLevel(tag.Read<TLowLevelType>(span), resolver);
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
        LowLevelType.Write(value, writer);
    }

    /// <summary>
    /// Write the key prefix for this attribute to the given writer
    /// </summary>
    protected void WritePrefix<TWriter>(EntityId entityId, AttributeCache attributeCache, TxId txId, bool isRetract, TWriter writer)
        where TWriter : IBufferWriter<byte>
    {
        var dbId = attributeCache.GetAttributeId(Id);
        var prefix = new KeyPrefix(entityId, dbId, txId, isRetract, LowLevelType);
        var span = writer.GetSpan(KeyPrefix.Size);
        MemoryMarshal.Write(span, prefix);
        writer.Advance(KeyPrefix.Size);
    }
}
