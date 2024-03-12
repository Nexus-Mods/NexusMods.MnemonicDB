using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns.LowLevel;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// A column backed by a single unsafe pointer. Used mostly for referencing off-heap memory that is pinned by
/// some storage system
/// </summary>
/// <param name="ptr"></param>
/// <typeparam name="T"></typeparam>
public sealed unsafe class OffHeapPacked<T>(byte *ptr) : IPacked<T> where T : struct
{
    public int Length => LowLevel->Length;

    internal LowLevelHeader* LowLevel => (LowLevelHeader*)ptr;

    public void CopyTo(int offset, Span<ulong> dest)
    {
        LowLevel->CopyTo(ptr, offset, dest);
    }

    public T this[int idx] => Unsafe.BitCast<ulong, T>(LowLevel->Get(ptr, idx));

    public void CopyTo(int offset, Span<T> dest)
    {
        CopyTo(offset, MemoryMarshal.Cast<T, ulong>(dest));
    }
}
