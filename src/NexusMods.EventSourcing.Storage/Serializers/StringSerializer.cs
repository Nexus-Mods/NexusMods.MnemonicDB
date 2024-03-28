using System;
using System.Buffers;
using System.Text;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Serializers;

public class StringSerializer : IValueSerializer<string>
{
    private static readonly Encoding _encoding = Encoding.UTF8;
    public Type NativeType => typeof(string);
    public Symbol UniqueId { get; } = Symbol.Intern<StringSerializer>();

    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return a.SequenceCompareTo(b);
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
