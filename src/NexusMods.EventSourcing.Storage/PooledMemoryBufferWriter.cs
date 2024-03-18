using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage;

/// <summary>
/// A IBufferWriter that uses pooled memory to reduce allocations.
/// </summary>
public sealed class PooledMemoryBufferWriter : IBufferWriter<byte>, IDisposable
{
    private readonly IMemoryOwner<byte> _owner;
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


    public ReadOnlyMemory<byte> WrittenMemory => _data.Slice(0, _idx);

    public Span<byte> WrittenSpanWritable => _data.Span.SliceFast(0, _idx);

    public Memory<byte> WrittenMemoryWritable => _data.Slice(0, _idx);

    private void Expand(int atLeast)
    {
        var newSize = _size;
        while (newSize < atLeast)
            newSize *= 2;

        var newData = new Memory<byte>(new byte[newSize]);
        _data.CopyTo(newData);
        _data = newData;
        _size = newSize;
    }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        if (_idx + count >= _size)
            Expand(_idx + count);
        Debug.Assert(_idx + count <= _size);
        _idx += count;
    }

    /// <inheritdoc />
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (_idx + sizeHint > _size)
            Expand(_idx + sizeHint);

        Debug.Assert(_idx + sizeHint <= _size);
        return _data[_idx..];
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (sizeHint == 0)
        {
            if (_idx >= _size)
                Expand(_idx + 1);
            return _data.Span.SliceFast(_idx);
        }

        if (_idx + sizeHint >= _size)
            Expand(_idx + sizeHint);

        Debug.Assert(_idx + sizeHint <= _size);
        return _data.Span.SliceFast(_idx, sizeHint);
    }

    /// <summary>
    /// Gets the written length of the data
    /// </summary>
    public int Length => _idx;

    public void Dispose()
    {
        _owner.Dispose();
    }
}
