using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments.Columns;

public abstract class ASimpleColumn<T> : IColumn
    where T : unmanaged
{
    public abstract T GetValue(in KeyPrefix prefix);

    public unsafe int FixedSize => sizeof(T);
    public Type ValueType => typeof(T);

    public void Extract(ReadOnlySpan<byte> src, ReadOnlySpan<byte> valueSpan, Span<byte> dst,
        PooledMemoryBufferWriter writer)
    {
        var prefix = KeyPrefix.Read(src);
        var value = GetValue(prefix);
        MemoryMarshal.Write(dst, value);
    }
}
