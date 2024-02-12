using System;
using System.Buffers;
using System.Text;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore.BuiltInSerializers;

public class StringSerializer : IValueSerializer<string>
{
    public Type NativeType => typeof(string);

    private static readonly UInt128 Id = "5B235332-EB1A-4E0F-9B43-B3F04B8D570D".ToUInt128Guid();

    public UInt128 UniqueId => Id;
    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return string.Compare(Encoding.UTF8.GetString(a), Encoding.UTF8.GetString(b), StringComparison.Ordinal);
    }

    public void Write<TWriter>(string value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var size = Encoding.UTF8.GetByteCount(value);
        var span = buffer.GetSpan(size);
        Encoding.UTF8.GetBytes(value, span);
        buffer.Advance(size);
    }

    public int Read(ReadOnlySpan<byte> buffer, out string val)
    {
        val = Encoding.UTF8.GetString(buffer);
        return buffer.Length;
    }
}
