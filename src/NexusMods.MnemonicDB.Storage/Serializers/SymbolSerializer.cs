using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage.Serializers;

internal class SymbolSerializer : IValueSerializer<Symbol>
{
    private static readonly Encoding _encoding = Encoding.ASCII;

    public static Symbol Id { get; } = Symbol.Intern<SymbolSerializer>();

    public Type NativeType => typeof(Symbol);
    public LowLevelTypes LowLevelType => LowLevelTypes.Ascii;

    public Symbol UniqueId => Id;


    public Symbol Read(in KeyPrefix prefix, ReadOnlySpan<byte> valueSpan)
    {
        if (prefix.ValueLength != KeyPrefix.LengthOversized)
            return Symbol.InternPreSanitized(_encoding.GetString(valueSpan.SliceFast(0, prefix.ValueLength)));

        var length = MemoryMarshal.Read<uint>(valueSpan);
        return Symbol.InternPreSanitized(_encoding.GetString(valueSpan.SliceFast(sizeof(uint), (int)length)));
    }

    public void Serialize<TWriter>(ref KeyPrefix prefix, Symbol value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var id = value.Id;
        var size = id.Length;
        if (size <= KeyPrefix.MaxLength)
        {
            var span = buffer.GetSpan(size);
            _encoding.GetBytes(id, span);
            buffer.Advance(size);
            prefix.ValueLength = (byte)size;
            prefix.LowLevelType = LowLevelTypes.Ascii;
        }
        else
        {
            var span = buffer.GetSpan(size + sizeof(uint));
            MemoryMarshal.Write(span, (uint)size);
            _encoding.GetBytes(id, span.SliceFast(sizeof(uint)));
            buffer.Advance(size + sizeof(uint));
            prefix.ValueLength = KeyPrefix.LengthOversized;
            prefix.LowLevelType = LowLevelTypes.Ascii;

        }
    }
}
