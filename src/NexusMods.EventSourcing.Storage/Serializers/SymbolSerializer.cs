using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Serializers;

public class SymbolSerializer : IValueSerializer<Symbol>
{
    public Type NativeType => typeof(Symbol);
    public Symbol UniqueId => Id;

    public static Symbol Id { get; } = Symbol.Intern<SymbolSerializer>();

    public int Compare<TDatomA, TDatomB>(in TDatomA a, in TDatomB b)
        where TDatomA : IRawDatom where TDatomB : IRawDatom
    {
        Span<byte> tmp = stackalloc byte[8];
        string aStr, bStr;
        if (a.Flags.HasFlag(DatomFlags.InlinedData))
        {
            BinaryPrimitives.WriteUInt64BigEndian(tmp, a.ValueLiteral);
            aStr = Encoding.UTF8.GetString(tmp);
        }
        else
        {
            aStr = Encoding.UTF8.GetString(a.ValueSpan);
        }

        if (b.Flags.HasFlag(DatomFlags.InlinedData))
        {
            BinaryPrimitives.WriteUInt64BigEndian(tmp, b.ValueLiteral);
            bStr = Encoding.UTF8.GetString(tmp);
        }
        else
        {
            bStr = Encoding.UTF8.GetString(b.ValueSpan);
        }

        return string.Compare(aStr, bStr, StringComparison.Ordinal);
    }

    public void Write<TWriter>(Symbol value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public int Read(ReadOnlySpan<byte> buffer, out Symbol val)
    {
        throw new NotImplementedException();
    }

    public bool Serialize<TWriter>(Symbol value, TWriter buffer, out ulong valueLiteral) where TWriter : IBufferWriter<byte>
    {
        var count = Encoding.UTF8.GetByteCount(value.Id);
        // Strings of 8 bytes or less can be inlined
        if (count <= 8)
        {
            Span<byte> stackSpan = stackalloc byte[8];
            Encoding.UTF8.GetBytes(value.Id, stackSpan);
            valueLiteral = BitConverter.ToUInt64(stackSpan);
            return true;
        }

        var span = buffer.GetSpan(count);
        var bytesWritten = Encoding.UTF8.GetBytes(value.Id, span);
        buffer.Advance(bytesWritten);
        valueLiteral = (ulong)bytesWritten;
        return false;
    }
}
