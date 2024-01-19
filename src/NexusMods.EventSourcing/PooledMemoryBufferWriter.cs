using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing;

/// <summary>
/// A IBufferWriter that uses pooled memory to reduce allocations.
/// </summary>
public sealed class PooledMemoryBufferWriter : IBufferWriter<byte>
{
    private IMemoryOwner<byte> _owner;
    private Memory<byte> _data;
    private int _idx;
    private int _size;

    /// <summary>
    /// Constructs a new pooled memory buffer writer, with the given initial capacity.
    /// </summary>
    /// <param name="initialCapacity"></param>
    public PooledMemoryBufferWriter(int initialCapacity = 1024)
    {
        _owner = MemoryPool<byte>.Shared.Rent(initialCapacity);
        _data = _owner.Memory;
        _idx = 0;
        _size = initialCapacity;
    }

    /// <summary>
    /// Resets the buffer writer, allowing it to be reused.
    /// </summary>
    public void Reset()
    {
        _idx = 0;
    }

    /// <summary>
    /// Gets the written span.
    /// </summary>
    /// <returns></returns>
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


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        if (_idx + count > _size)
            Expand();
        _idx += count;
    }

    /// <inheritdoc />
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (_idx + sizeHint > _size)
            Expand();
        return _data[_idx..];
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (_idx + sizeHint > _size)
            Expand();
        return _data.Span.SliceFast(_idx);
    }
}
