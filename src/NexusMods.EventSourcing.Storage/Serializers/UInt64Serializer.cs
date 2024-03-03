
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Serializers;

public class UInt64Serializer : IValueSerializer<ulong>
{
    public uint Serialize<TWriter>(in TWriter writer, in ulong value, out ulong inlineValue)
    {
        inlineValue = value;
        return sizeof(ulong);
    }

    public Type NativeType => typeof(ulong);
    public Symbol UniqueId { get; } = Symbol.Intern<UInt64Serializer>();

    public int Compare(in Datom a, in Datom b)
    {
        return a.Unmarshal<ulong>().CompareTo(b.Unmarshal<ulong>());
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
}
