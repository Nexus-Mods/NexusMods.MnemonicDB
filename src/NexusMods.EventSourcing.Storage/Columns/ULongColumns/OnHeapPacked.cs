using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

public class OnHeapPacked<T>(IMemoryOwner<byte> memory) : IPacked<T> where T : struct
{
    public int Length => LowLevel.Length;

    internal ref LowLevelPacked LowLevel => ref MemoryMarshal.AsRef<LowLevelPacked>(memory.Memory.Span);

    internal unsafe Span<byte> Data => memory.Memory.Span.SliceFast(sizeof(LowLevelPacked));

    public void CopyTo(int offset, Span<ulong> dest)
    {
        throw new NotImplementedException();
    }

    public T this[int idx] => Unsafe.BitCast<ulong, T>(LowLevel.Get(Data, idx));

    public void CopyTo(int offset, Span<T> dest)
    {
        for (var i = 0; i < Length; i++)
        {
            dest[i] = this[i];
        }
    }
}
