using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB.Storage;

/// <summary>
///     A IBufferWriter that uses pooled memory to reduce allocations.
/// </summary>
public sealed class PooledMemoryBufferWriter : IBufferWriter<byte>, IDisposable
{
    private readonly IMemoryOwner<byte> _owner;
    private Memory<byte> _data;
    private int _size;

    /// <summary>
    ///     Constructs a new pooled memory buffer writer, with the given initial capacity.
    /// </summary>
    /// <param name="initialCapacity"></param>
    public PooledMemoryBufferWriter(int initialCapacity = 1024)
    {
        _owner = MemoryPool<byte>.Shared.Rent(initialCapacity);
        _data = _owner.Memory;
        Length = 0;
        _size = initialCapacity;
    }


    public ReadOnlyMemory<byte> WrittenMemory => _data.Slice(0, Length);

    public Span<byte> WrittenSpanWritable => _data.Span.SliceFast(0, Length);

    /// <summary>
    ///     Gets the written length of the data
    /// </summary>
    public int Length { get; private set; }


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        if (Length + count >= _size)
            Expand(Length + count);
        Debug.Assert(Length + count <= _size);
        Length += count;
    }

    /// <inheritdoc />
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (Length + sizeHint > _size)
            Expand(Length + sizeHint);

        Debug.Assert(Length + sizeHint <= _size);
        return _data[Length..];
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (sizeHint == 0)
        {
            if (Length >= _size)
                Expand(Length + 1);
            return _data.Span.SliceFast(Length);
        }

        if (Length + sizeHint >= _size)
            Expand(Length + sizeHint);

        Debug.Assert(Length + sizeHint <= _size);
        return _data.Span.SliceFast(Length, sizeHint);
    }

    public void Dispose()
    {
        _owner.Dispose();
    }

    /// <summary>
    ///     Resets the buffer writer, allowing it to be reused.
    /// </summary>
    public void Reset()
    {
        Length = 0;
    }

    /// <summary>
    ///     Gets the written span.
    /// </summary>
    /// <returns></returns>
    public ReadOnlySpan<byte> GetWrittenSpan()
    {
        return _data.Span.SliceFast(0, Length);
    }


    /// <summary>
    ///     Gets the written span, but allows it to be written to.
    /// </summary>
    public Span<byte> GetWrittenSpanWritable()
    {
        return _data.Span.SliceFast(0, Length);
    }

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

    public void Rewind(int length)
    {
        Length -= length;
    }
}
