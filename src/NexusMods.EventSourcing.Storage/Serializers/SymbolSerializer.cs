using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Serializers;

public class SymbolSerializer : IValueSerializer<Symbol>
{
    private static readonly Encoding _encoding = Encoding.UTF8;
    public Type NativeType => typeof(Symbol);
    public Symbol UniqueId => Id;

    public static Symbol Id { get; } = Symbol.Intern<SymbolSerializer>();

    public int Compare(in Datom a, in Datom b)
    {
        // TODO: This can likely be vectorized so it keeps the strings in their original locations
        return string.Compare(_encoding.GetString(a.V.Span), _encoding.GetString(b.V.Span), StringComparison.Ordinal);
    }

    public void Write<TWriter>(Symbol value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public int Read(ReadOnlySpan<byte> buffer, out Symbol val)
    {
        val = Symbol.Intern(_encoding.GetString(buffer));
        return _encoding.GetByteCount(val.Id);
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
