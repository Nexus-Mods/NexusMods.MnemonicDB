using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Serializers;

public class UInt64Serializer : IValueSerializer<ulong>
{
    public Type NativeType => typeof(ulong);
    public Symbol UniqueId { get; } = Symbol.Intern<UInt64Serializer>();

    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<ulong>(a).CompareTo(MemoryMarshal.Read<ulong>(b));
    }

    public void Write<TWriter>(ulong value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public int Read(ReadOnlySpan<byte> buffer, out ulong val)
    {
        val = MemoryMarshal.Read<ulong>(buffer);
        return sizeof(ulong);
    }

    public void Serialize<TWriter>(ulong value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(sizeof(ulong));
        MemoryMarshal.Write(span, value);
        buffer.Advance(sizeof(ulong));
    }

    public uint Serialize<TWriter>(in TWriter writer, in ulong value, out ulong inlineValue)
    {
        inlineValue = value;
        return sizeof(ulong);
    }
}
