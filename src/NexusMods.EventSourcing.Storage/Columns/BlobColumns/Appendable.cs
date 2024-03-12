using System;
using System.Buffers;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

/// <summary>
/// An appendable blob column. This column allows for appending of new spans of data.
/// </summary>
public class Appendable : IReadable, IUnpacked, IAppendable
{
    private IMemoryOwner<byte> _memoryOwner;
    private Memory<byte> _memory;
    private ulong _currentOffset;

    private readonly ULongColumns.Appendable _offsets;
    private readonly ULongColumns.Appendable _lengths;

    private Appendable(int initialSize = 1024)
    {
        _memoryOwner = MemoryPool<byte>.Shared.Rent(initialSize);
        _memory = _memoryOwner.Memory;
        _currentOffset = 0;

        _offsets = ULongColumns.Appendable.Create();
        _lengths = ULongColumns.Appendable.Create();
    }

    public static Appendable Create(int initialSize = 1024) => new(initialSize);

    public int Count => _offsets.Length;

    public ReadOnlySpan<byte> this[int offset]
    {
        get
        {
            var length = _lengths.Span[offset];
            return _memory.Slice((int)_offsets.Span[offset], (int)length).Span;
        }
    }

    public ReadOnlyMemory<byte> GetMemory(int offset)
    {
        var length = _lengths.Span[offset];
        return _memory.Slice((int)_offsets.Span[offset], (int)length);
    }

/// <summary>
    /// Span to the raw memory used by the column.
    /// </summary>
    public ReadOnlySpan<byte> Span => _memory.Span.SliceFast(0, (int)_currentOffset);

    /// <summary>
    /// Span of offsets into the column for each value.
    /// </summary>
    public ULongColumns.IUnpacked Offsets => _offsets;

    /// <summary>
    /// Span of lengths for each value in the column.
    /// </summary>
    public ULongColumns.IUnpacked Lengths => _lengths;


    /// <summary>
    /// Expand the memory and append the span to the end of the memory.
    /// </summary>
    /// <param name="span"></param>
    public void Append(ReadOnlySpan<byte> span)
    {
        Ensure(span.Length);
        var destSpan = _memory.Span.Slice((int)_currentOffset, span.Length);
        span.CopyTo(destSpan);
        _offsets.Append(_currentOffset);
        _currentOffset += (ulong)span.Length;
        _lengths.Append((ulong)span.Length);
    }

    /// <summary>
    /// Ensure that at least spanLength bytes are available in the memory, otherwise expand the memory.
    /// </summary>
    /// <param name="spanLength"></param>
    private void Ensure(int spanLength)
    {
        if (_currentOffset + (ulong)spanLength <= (ulong)_memory.Length) return;
        var newMemory = MemoryPool<byte>.Shared.Rent(_memory.Length * 2);
        _memory[..(int)_currentOffset].CopyTo(newMemory.Memory);
        _memoryOwner.Dispose();
        _memoryOwner = newMemory;
        _memory = newMemory.Memory;
    }
}
