using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.ValueSerializers;

public class HashSerializer : IValueSerializer<Hash>
{
    public Type NativeType => typeof(Hash);
    public Symbol UniqueId => Symbol.Intern<HashSerializer>();

    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<Size>(a).CompareTo(MemoryMarshal.Read<Size>(b));
    }

    public void Write<TWriter>(Hash value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public int Read(ReadOnlySpan<byte> buffer, out Hash val)
    {
        val = MemoryMarshal.Read<Hash>(buffer);
        return Unsafe.SizeOf<Hash>();
    }

    public void Serialize<TWriter>(Hash value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(Unsafe.SizeOf<Hash>());
        MemoryMarshal.Write(span, in value);
        buffer.Advance(Unsafe.SizeOf<Hash>());
    }
}
