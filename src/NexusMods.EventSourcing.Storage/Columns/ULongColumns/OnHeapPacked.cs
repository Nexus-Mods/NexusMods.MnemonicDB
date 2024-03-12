using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns.LowLevel;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// A column backed by a single IMemoryOwner. Used for referencing on-heap memory that is managed by the GC.
/// </summary>
/// <param name="memory"></param>
/// <typeparam name="T"></typeparam>
public sealed class OnHeapPacked<T>(IMemoryOwner<byte> memory) : IPacked<T> where T : struct
{
    public int Length => LowLevel.Length;

    internal ref LowLevelHeader LowLevel => ref MemoryMarshal.AsRef<LowLevelHeader>(memory.Memory.Span);

    internal Span<byte> Span => memory.Memory.Span;

    public void CopyTo(int offset, Span<ulong> dest)
    {
        LowLevel.CopyTo(Span, offset, dest);
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
