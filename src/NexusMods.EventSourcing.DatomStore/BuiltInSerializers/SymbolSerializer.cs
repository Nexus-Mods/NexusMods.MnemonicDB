using System;
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;
using static System.Text.Encoding;

namespace NexusMods.EventSourcing.DatomStore.BuiltInSerializers;

public class SymbolSerializer : IValueSerializer<Symbol>
{
    public Type NativeType => typeof(Symbol);

    public static readonly UInt128 Id = "1BAE8D48-8775-4642-AEA9-9C925B30D4B2".ToUInt128Guid();
    public UInt128 UniqueId => Id;
    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return string.Compare(UTF8.GetString(a), UTF8.GetString(b), StringComparison.Ordinal);
    }

    public void Write<TWriter>(Symbol value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var size = UTF8.GetByteCount(value.Id);
        var span = buffer.GetSpan(size);
        UTF8.GetBytes(value.Id, span);
        buffer.Advance(size);
    }

    public int Read(ReadOnlySpan<byte> buffer, out Symbol val)
    {
        val = Symbol.Intern(UTF8.GetString(buffer));
        return buffer.Length;
    }
}
