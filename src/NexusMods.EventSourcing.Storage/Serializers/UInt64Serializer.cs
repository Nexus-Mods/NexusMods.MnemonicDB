
using System;
using System.Buffers;
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

    public int Compare<TDatomA, TDatomB>(in TDatomA a, in TDatomB b) where TDatomA : IRawDatom where TDatomB : IRawDatom
    {
        return a.ValueLiteral.CompareTo(b.ValueLiteral);
    }

    public void Write<TWriter>(ulong value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public int Read(ReadOnlySpan<byte> buffer, out ulong val)
    {
        throw new NotImplementedException();
    }

    public bool Serialize<TWriter>(ulong value, TWriter buffer, out ulong valueLiteral) where TWriter : IBufferWriter<byte>
    {
        valueLiteral = value;
        return true;
    }
}
