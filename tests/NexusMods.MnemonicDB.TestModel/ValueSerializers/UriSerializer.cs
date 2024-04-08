﻿using System.Buffers;
using System.Text;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.TestModel.ValueSerializers;

public class UriSerializer : IValueSerializer<Uri>
{
    private static readonly Encoding _encoding = Encoding.UTF8;
    public Type NativeType => typeof(Uri);
    public Symbol UniqueId => Symbol.Intern<UriSerializer>();

    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        return a.SequenceCompareTo(b);
    }

    public Uri Read(ReadOnlySpan<byte> buffer)
    {
        return new Uri(_encoding.GetString(buffer));
    }

    public void Serialize<TWriter>(Uri value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var strValue = value.ToString();
        // TODO: No reason to walk the string twice, we should do this in one pass
        var bytes = _encoding.GetByteCount(strValue);
        var span = buffer.GetSpan(bytes);
        _encoding.GetBytes(strValue, span);
        buffer.Advance(bytes);
    }
}
