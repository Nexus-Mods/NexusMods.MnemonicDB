using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Serializers;

public abstract class AUnmanagedSerializer<TSrc, TConverted>(LowLevelTypes lowLevelType, byte fixedSize) : IValueSerializer<TSrc>
    where TConverted : unmanaged
{
    /// <summary>
    /// Convert from the high level type to the low level type.
    /// </summary>
    protected abstract TConverted ToLowLevel(TSrc src);

    /// <summary>
    /// Convert from the low level type to the high level type.
    /// </summary>
    protected abstract TSrc FromLowLevel(TConverted src);

    public Type NativeType => typeof(TSrc);
    public LowLevelTypes LowLevelType => lowLevelType;
    public abstract Symbol UniqueId { get; }
    public TSrc Read(in KeyPrefix prefix, ReadOnlySpan<byte> valueSpan)
    {
        Debug.Assert(prefix.ValueLength == fixedSize && prefix.LowLevelType == lowLevelType);
        return FromLowLevel(MemoryMarshal.Read<TConverted>(valueSpan));
    }

    public void Serialize<TWriter>(ref KeyPrefix prefix, TSrc value, TWriter buffer)
        where TWriter : IBufferWriter<byte>
    {
        prefix.ValueLength = fixedSize;
        prefix.LowLevelType = lowLevelType;
        var span = buffer.GetSpan(KeyPrefix.Size + fixedSize);
        MemoryMarshal.Write(span, prefix);
        MemoryMarshal.Write(span.SliceFast(KeyPrefix.Size), ToLowLevel(value));
        buffer.Advance(KeyPrefix.Size + fixedSize);
    }
}
