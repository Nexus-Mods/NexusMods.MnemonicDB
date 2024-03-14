using System;
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Columns.BlobColumns;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;

namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

/// <summary>
/// An appendable blob column. This column allows for appending of new spans of data.
/// </summary>
public class Appendable : IReadable, IUnpacked, IAppendable
{
    private readonly ULongColumns.Appendable _offsets;
    private readonly ULongColumns.Appendable _lengths;
    private readonly PooledMemoryBufferWriter _writer;

    private Appendable(int initialSize = 1024)
    {
        _writer = new PooledMemoryBufferWriter();

        _offsets = ULongColumns.Appendable.Create();
        _lengths = ULongColumns.Appendable.Create();
    }

    public static Appendable Create(int initialSize = 1024) => new(initialSize);

    public static Appendable Create(IReadable src, int offset, int length)
    {
        var appendable = Appendable.Create();
        for (var i = 0; i < length; i++)
        {
            var span = src[offset + i];
            appendable.Append(span);
        }
        return appendable;
    }

    public int Count => _offsets.Length;

    public ReadOnlySpan<byte> this[int offset]
    {
        get
        {
            var length = _lengths.Span[offset];
            return _writer.GetWrittenSpan().Slice((int)_offsets.Span[offset], (int)length);
        }
    }

    public ReadOnlyMemory<byte> Memory => _writer.WrittenMemory;

    EventSourcing.Abstractions.Columns.ULongColumns.IReadable IReadable.LengthsColumn => Lengths;

    EventSourcing.Abstractions.Columns.ULongColumns.IReadable IReadable.OffsetsColumn => Offsets;

    public IUnpacked Unpack()
    {
        throw new NotImplementedException();
    }

    public ReadOnlyMemory<byte> GetMemory(int offset)
    {
        var length = _lengths.Span[offset];
        return _writer.WrittenMemory.Slice((int)_offsets.Span[offset], (int)length);
    }

    /// <summary>
    /// Span to the raw memory used by the column.
    /// </summary>
    public ReadOnlySpan<byte> Span => _writer.GetWrittenSpan();

    /// <summary>
    /// Span of offsets into the column for each value.
    /// </summary>
    public EventSourcing.Abstractions.Columns.ULongColumns.IUnpacked Offsets => _offsets;

    /// <summary>
    /// Span of lengths for each value in the column.
    /// </summary>
    public EventSourcing.Abstractions.Columns.ULongColumns.IUnpacked Lengths => _lengths;

    public IReadable Pack()
    {
        return new BlobPackedColumn
        {
            Count = Count,
            Offsets = (ULongPackedColumn)Offsets.Pack(),
            Lengths = (ULongPackedColumn)Lengths.Pack(),
            Data = Span.ToArray()
        };
    }




    /// <summary>
    /// Expand the memory and append the span to the end of the memory.
    /// </summary>
    /// <param name="span"></param>
    public void Append(ReadOnlySpan<byte> span)
    {
        _offsets.Append((ulong)_writer.Length);
        _lengths.Append((ulong)span.Length);
        _writer.Write(span);
    }

    public void Append<T>(IValueSerializer<T> serializer, T value)
    {
        var offset = _writer.Length;
        serializer.Write(value, _writer);
        var length = _writer.Length - offset;
        _offsets.Append((ulong)offset);
        _lengths.Append((ulong)length);
    }
}
