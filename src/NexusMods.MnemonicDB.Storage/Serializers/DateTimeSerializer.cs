using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Storage.Serializers;

internal class DateTimeSerializer : IValueSerializer<DateTime>
{
    public Type NativeType => typeof(DateTime);
    public Symbol UniqueId => Symbol.Intern<DateTimeSerializer>();
    public int Compare(in ReadOnlySpan<byte> a, in ReadOnlySpan<byte> b)
    {
        var aTime = MemoryMarshal.Read<DateTime>(a);
        var bTime = MemoryMarshal.Read<DateTime>(b);
        return aTime.CompareTo(bTime);
    }

    public DateTime Read(ReadOnlySpan<byte> buffer)
    {
        return MemoryMarshal.Read<DateTime>(buffer);
    }

    public void Serialize<TWriter>(DateTime value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        unsafe
        {
            var span = buffer.GetSpan(sizeof(DateTime));
            MemoryMarshal.Write(span, ref value);
            buffer.Advance(sizeof(DateTime));
        }
    }
}
