using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.TestModel.ValueSerializers;

public class SizeSerializer : IValueSerializer<Size>
{
    public Type NativeType => typeof(Size);
    public Symbol UniqueId => Symbol.Intern<SizeSerializer>();

    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<Size>(a).CompareTo(MemoryMarshal.Read<Size>(b));
    }

    public void Write<TWriter>(Size value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public int Read(ReadOnlySpan<byte> buffer, out Size val)
    {
        val = MemoryMarshal.Read<Size>(buffer);
        return Unsafe.SizeOf<Size>();
    }

    public void Serialize<TWriter>(Size value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(Unsafe.SizeOf<Size>());
        MemoryMarshal.Write(span, in value);
        buffer.Advance(Unsafe.SizeOf<Size>());
    }
}
