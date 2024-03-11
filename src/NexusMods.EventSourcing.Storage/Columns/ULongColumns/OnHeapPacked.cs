using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

public sealed class OnHeapPacked<T>(IMemoryOwner<byte> memory) : IPacked<T> where T : struct
{
    public int Length => LowLevel.Length;

    internal ref LowLevelHeader LowLevel => ref MemoryMarshal.AsRef<LowLevelHeader>(memory.Memory.Span);

    internal Span<byte> Span => memory.Memory.Span;

    public void CopyTo(int offset, Span<ulong> dest)
    {
        LowLevel.CopyTo(offset, dest);
    }

    public T this[int idx] => Unsafe.BitCast<ulong, T>(LowLevel.Get(memory.Memory.Span, idx));

    public void CopyTo(int offset, Span<T> dest)
    {
        CopyTo(offset, MemoryMarshal.Cast<T, ulong>(dest));
    }

    public void Dispose()
    {
        memory.Dispose();
    }
}
