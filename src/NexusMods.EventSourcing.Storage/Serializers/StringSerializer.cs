using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Serializers;

public class StringSerializer : IValueSerializer<string>
{
    private static Encoding _encoding = Encoding.UTF8;
    public Type NativeType => typeof(string);
    public Symbol UniqueId { get; } = Symbol.Intern<StringSerializer>();

    public int Compare(in Datom a, in Datom b)
    {
        // TODO: This can likely be vectorized so it keeps the strings in their original locations
        return string.Compare(_encoding.GetString(a.V.Span), _encoding.GetString(b.V.Span), StringComparison.Ordinal);
    }

    public void Write<TWriter>(string value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public int Read(ReadOnlySpan<byte> buffer, out string val)
    {
        val = _encoding.GetString(buffer);
        return buffer.Length;
    }

    public void Serialize<TWriter>(string value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        // TODO: No reason to walk the string twice, we should do this in one pass
        var bytes = _encoding.GetByteCount(value);
        var span = buffer.GetSpan(bytes);
        _encoding.GetBytes(value, span);
        buffer.Advance(bytes);
    }
}
