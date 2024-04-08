using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Storage.Serializers;

internal class UInt64Serializer : IValueSerializer<ulong>
{
    public Type NativeType => typeof(ulong);
    public Symbol UniqueId { get; } = Symbol.Intern<UInt64Serializer>();

    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<ulong>(a).CompareTo(MemoryMarshal.Read<ulong>(b));
    }
    public ulong Read(ReadOnlySpan<byte> buffer)
    {
        return MemoryMarshal.Read<ulong>(buffer);
    }

    public void Serialize<TWriter>(ulong value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(sizeof(ulong));
        MemoryMarshal.Write(span, value);
        buffer.Advance(sizeof(ulong));
    }
}
