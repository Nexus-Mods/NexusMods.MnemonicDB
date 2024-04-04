using System.Buffers;
using System.Text;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.ValueSerializers;

public class RelativePathSerializer : IValueSerializer<RelativePath>
{
    private static readonly Encoding _encoding = Encoding.UTF8;

    public Type NativeType => typeof(RelativePath);
    public Symbol UniqueId => Symbol.Intern<RelativePathSerializer>();

    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return a.SequenceCompareTo(b);
    }

    public void Write<TWriter>(RelativePath value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public int Read(ReadOnlySpan<byte> buffer, out RelativePath val)
    {
        val = _encoding.GetString(buffer);
        return buffer.Length;
    }

    public void Serialize<TWriter>(RelativePath value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        // TODO: No reason to walk the string twice, we should do this in one pass
        var bytes = _encoding.GetByteCount(value);
        var span = buffer.GetSpan(bytes);
        _encoding.GetBytes(value, span);
        buffer.Advance(bytes);
    }
}
