using System;
using System.Buffers;
using System.Text;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Storage.Serializers;

internal class SymbolSerializer : IValueSerializer<Symbol>
{
    private static readonly Encoding _encoding = Encoding.UTF8;

    public static Symbol Id { get; } = Symbol.Intern<SymbolSerializer>();

    public Type NativeType => typeof(Symbol);

    public Symbol UniqueId => Id;

    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return a.SequenceCompareTo(b);
    }

    public Symbol Read(ReadOnlySpan<byte> buffer)
    {
        return Symbol.InternPreSanitized(_encoding.GetString(buffer));
    }

    public void Serialize<TWriter>(Symbol value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        // TODO: No reason to walk the string twice, we should do this in one pass
        var bytes = _encoding.GetByteCount(value.Id);
        var span = buffer.GetSpan(bytes);
        _encoding.GetBytes(value.Id, span);
        buffer.Advance(bytes);
    }
}
