using System;
using System.Buffers;
using System.Text;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Storage.Serializers;

/// <inheritdoc />
public class StringSerializer : IValueSerializer<string>
{
    private static readonly Encoding _encoding = Encoding.UTF8;

    /// <inheritdoc />
    public Type NativeType => typeof(string);

    /// <inheritdoc />
    public Symbol UniqueId { get; } = Symbol.Intern<StringSerializer>();

    /// <inheritdoc />
    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return a.SequenceCompareTo(b);
    }

    /// <inheritdoc />
    public string Read(ReadOnlySpan<byte> buffer)
    {
        return _encoding.GetString(buffer);
    }

    /// <inheritdoc />
    public void Serialize<TWriter>(string value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        // TODO: No reason to walk the string twice, we should do this in one pass
        var bytes = _encoding.GetByteCount(value);
        var span = buffer.GetSpan(bytes);
        _encoding.GetBytes(value, span);
        buffer.Advance(bytes);
    }
}
