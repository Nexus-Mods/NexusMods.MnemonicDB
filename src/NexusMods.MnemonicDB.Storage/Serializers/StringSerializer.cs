using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage.Serializers;

/// <inheritdoc />
public class StringSerializer : IValueSerializer<string>
{
    private static readonly Encoding _encoding = Encoding.UTF8;

    /// <inheritdoc />
    public Type NativeType => typeof(string);

    public LowLevelTypes LowLevelType => LowLevelTypes.Utf8;

    /// <inheritdoc />
    public Symbol UniqueId { get; } = Symbol.Intern<StringSerializer>();

    public string Read(in KeyPrefix prefix, ReadOnlySpan<byte> valueSpan)
    {
        if (prefix.ValueLength != KeyPrefix.LengthOversized)
            return _encoding.GetString(valueSpan.SliceFast(0, prefix.ValueLength));

        var length = MemoryMarshal.Read<uint>(valueSpan);
        return _encoding.GetString(valueSpan.SliceFast(sizeof(uint), (int)length));
    }

    public void Serialize<TWriter>(ref KeyPrefix prefix, string value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var size = _encoding.GetByteCount(value);
        if (size <= KeyPrefix.MaxLength)
        {
            var span = buffer.GetSpan(size + KeyPrefix.Size);
            _encoding.GetBytes(value, span);
            buffer.Advance(size);
            prefix.ValueLength = (byte)size;
            prefix.LowLevelType = LowLevelTypes.Utf8;
            buffer.Advance(size + KeyPrefix.Size);
        }
        else
        {
            var span = buffer.GetSpan(size + sizeof(uint) + KeyPrefix.Size);
            MemoryMarshal.Write(span, (uint)size);
            _encoding.GetBytes(value, span.SliceFast(sizeof(uint)));
            buffer.Advance(size + sizeof(uint));
            prefix.ValueLength = KeyPrefix.LengthOversized;
            prefix.LowLevelType = LowLevelTypes.Utf8;
            buffer.Advance(size + sizeof(uint) + KeyPrefix.Size);


        }
    }
}
