using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Serializers;

public abstract class AUtf8Serializer<T>(bool caseSensitive = true) : IValueSerializer<T>
{
    private static readonly Encoding _encoding = Encoding.UTF8;

    /// <summary>
    /// Converts the value to a span of characters.
    /// </summary>
    protected abstract ReadOnlySpan<char> ToSpan(T value);

    /// <summary>
    /// Converts the span of characters to a value.
    /// </summary>
    protected abstract T FromSpan(ReadOnlySpan<char> span);

    /// <inheritdoc />
    public Type NativeType => typeof(T);

    /// <inheritdoc />
    public LowLevelTypes LowLevelType => caseSensitive ? LowLevelTypes.Utf8 : LowLevelTypes.InsensitiveUtf8;

    /// <inheritdoc />
    public abstract Symbol UniqueId { get; }

    /// <inheritdoc />
    public T Read(in KeyPrefix prefix, ReadOnlySpan<byte> valueSpan)
    {
        if (prefix.ValueLength != KeyPrefix.LengthOversized)
            return FromSpan(_encoding.GetString(valueSpan.SliceFast(0, prefix.ValueLength)));

        var length = MemoryMarshal.Read<uint>(valueSpan);
        // Not optimal, but whatever
        return FromSpan(_encoding.GetString(valueSpan.SliceFast(sizeof(uint), (int)length)));
    }

    /// <inheritdoc />
    public void Serialize<TWriter>(ref KeyPrefix prefix, T value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var charSpan = ToSpan(value);
        var size = _encoding.GetByteCount(charSpan);
        if (size <= KeyPrefix.MaxLength)
        {
            var span = buffer.GetSpan(size + KeyPrefix.Size);
            _encoding.GetBytes(charSpan, span);
            buffer.Advance(size);
            prefix.ValueLength = (byte)size;
            prefix.LowLevelType = LowLevelTypes.Utf8;
            buffer.Advance(size + KeyPrefix.Size);
        }
        else
        {
            var span = buffer.GetSpan(size + sizeof(uint) + KeyPrefix.Size);
            MemoryMarshal.Write(span, (uint)size);
            _encoding.GetBytes(charSpan, span.SliceFast(sizeof(uint)));
            buffer.Advance(size + sizeof(uint));
            prefix.ValueLength = KeyPrefix.LengthOversized;
            prefix.LowLevelType = LowLevelTypes.Utf8;
            buffer.Advance(size + sizeof(uint) + KeyPrefix.Size);
        }
    }
}
