using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing;

public sealed class PooledMemoryBufferWriter : IBufferWriter<byte>
{
    private IMemoryOwner<byte> _owner;
    private Memory<byte> _data;
    private int _idx;
    private int _size;

    public PooledMemoryBufferWriter(int initialCapacity = 1024)
    {
        _owner = MemoryPool<byte>.Shared.Rent(initialCapacity);
        _data = _owner.Memory;
        _idx = 0;
        _size = initialCapacity;
    }

    public void Reset()
    {
        _idx = 0;
    }

    public ReadOnlySpan<byte> GetWrittenSpan() => _data.Span.SliceFast(0, _idx);

    private void Expand()
    {
        var newSize = _data.Length * 2;
        var newData = MemoryPool<byte>.Shared.Rent(newSize);
        _data.CopyTo(newData.Memory);
        _owner.Dispose();
        _owner = newData;
        _data = newData.Memory;
        _size = newSize;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        if (_idx + count > _size)
            Expand();
        _idx += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (_idx + sizeHint > _size)
            Expand();
        return _data[_idx..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (_idx + sizeHint > _size)
            Expand();
        return _data.Span.SliceFast(_idx);
    }
}
