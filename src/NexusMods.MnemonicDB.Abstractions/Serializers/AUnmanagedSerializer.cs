using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Serializers;

public abstract class AUnmanagedSerializer<T>(LowLevelTypes lowLevelType, byte fixedSize) : IValueSerializer<T>
    where T : unmanaged
{
    public Type NativeType => typeof(T);
    public LowLevelTypes LowLevelType => lowLevelType;
    public abstract Symbol UniqueId { get; }
    public T Read(in KeyPrefix prefix, ReadOnlySpan<byte> valueSpan)
    {
        Debug.Assert(prefix.ValueLength == fixedSize && prefix.LowLevelType == lowLevelType);
        return MemoryMarshal.Read<T>(valueSpan);
    }

    public void Serialize<TWriter>(ref KeyPrefix prefix, T value, TWriter buffer)
        where TWriter : IBufferWriter<byte>
    {
        prefix.ValueLength = fixedSize;
        prefix.LowLevelType = lowLevelType;
        var span = buffer.GetSpan(KeyPrefix.Size + fixedSize);
        MemoryMarshal.Write(span, prefix);
        MemoryMarshal.Write(span.SliceFast(KeyPrefix.Size), value);
        buffer.Advance(KeyPrefix.Size + fixedSize);
    }
}
