using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Serializers;

public class TxIdSerializer : IValueSerializer<TxId>
{
    public Type NativeType => typeof(TxId);
    public Symbol UniqueId { get; } = Symbol.Intern<TxIdSerializer>();

    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<TxId>(a).CompareTo(MemoryMarshal.Read<TxId>(b));
    }

    public void Write<TWriter>(TxId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public int Read(ReadOnlySpan<byte> buffer, out TxId val)
    {
        val = MemoryMarshal.Read<TxId>(buffer);
        return sizeof(ulong);
    }

    public void Serialize<TWriter>(TxId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(sizeof(ulong));
        MemoryMarshal.Write(span, value.Value);
        buffer.Advance(sizeof(ulong));
    }
}
