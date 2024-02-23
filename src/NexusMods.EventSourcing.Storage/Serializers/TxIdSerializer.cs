using System;
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Serializers;

public class TxIdSerializer : IValueSerializer<TxId>
{
    public Type NativeType => typeof(TxId);
    public Symbol UniqueId { get; } = Symbol.Intern<TxIdSerializer>();
    public int Compare<TDatomA, TDatomB>(in TDatomA a, in TDatomB b) where TDatomA : IRawDatom where TDatomB : IRawDatom
    {
        return a.ValueLiteral.CompareTo(b.ValueLiteral);
    }

    public void Write<TWriter>(TxId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public int Read(ReadOnlySpan<byte> buffer, out TxId val)
    {
        throw new NotImplementedException();
    }

    public bool Serialize<TWriter>(TxId value, TWriter buffer, out ulong valueLiteral) where TWriter : IBufferWriter<byte>
    {
        valueLiteral = value.Value;
        return true;
    }
}
