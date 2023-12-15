using System;
using System.Buffers;

namespace NexusMods.EventSourcing;

public class PooledMemoryBufferWriter : IBufferWriter<byte>
{
    private IMemoryOwner<byte> _data;
    private int _idx;

    public PooledMemoryBufferWriter(int initialCapacity)
    {
        _data = MemoryPool<byte>.Shared.Rent(initialCapacity);
        _idx = 0;
    }

    public void Reset()
    {
        _idx = 0;
    }

    public ReadOnlySpan<byte> GetWrittenSpan() => _data.Memory[.._idx].Span;

    private void Expand()
    {
        var newSize = _data.Memory.Length * 2;
        var newData = MemoryPool<byte>.Shared.Rent(newSize);
        _data.Memory.CopyTo(newData.Memory);
        _data.Dispose();
        _data = newData;
    }


    public void Advance(int count)
    {
        if (_idx + count > _data.Memory.Length)
            Expand();
        _idx += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (_idx + sizeHint > _data.Memory.Length)
            Expand();
        return _data.Memory[_idx..];
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (_idx + sizeHint > _data.Memory.Length)
            Expand();
        return _data.Memory.Span[_idx..];
    }
}
