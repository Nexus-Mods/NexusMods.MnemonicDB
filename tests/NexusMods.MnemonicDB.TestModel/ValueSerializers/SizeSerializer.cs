using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.ValueSerializers;

public class SizeSerializer : IValueSerializer<Size>
{
    public Type NativeType => typeof(Size);
    public Symbol UniqueId => Symbol.Intern<SizeSerializer>();

    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<Size>(a).CompareTo(MemoryMarshal.Read<Size>(b));
    }

    public Size Read(ReadOnlySpan<byte> buffer)
    {
        return MemoryMarshal.Read<Size>(buffer);
    }

    public void Serialize<TWriter>(Size value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(Unsafe.SizeOf<Size>());
        MemoryMarshal.Write(span, in value);
        buffer.Advance(Unsafe.SizeOf<Size>());
    }
}
