using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Columns.PackedColumns;

namespace NexusMods.EventSourcing.Storage.Columns;

public class AppendableBlobColumn : IAppendableBlobColumn
{
    private readonly PooledMemoryBufferWriter _writer = new();
    private readonly UnsignedIntegerColumn<uint> _offsets = new();
    private readonly UnsignedIntegerColumn<uint> _sizes = new();

    public ReadOnlyMemory<byte> this[int idx] => _writer.WrittenMemory.Slice((int)_offsets[idx], (int)_sizes[idx]);

    public int Length => _offsets.Length;

    public IBlobColumn Pack()
    {
        return new PackedBlobColumn(_writer.WrittenMemory,
            _offsets.Pack(),
            _sizes.Pack());
    }

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new NotImplementedException();
    }

    public void Append(ReadOnlySpan<byte> value)
    {
        var offset = _writer.Length;
        var newSpan = _writer.GetSpan(value.Length);
        value.CopyTo(newSpan);

        _writer.Advance(value.Length);
        _offsets.Append((uint)offset);
        _sizes.Append((uint)value.Length);
    }

    /// <summary>
    /// Append a value to the column, after feeding it through the given serializer
    /// </summary>
    /// <param name="serializer"></param>
    /// <param name="value"></param>
    /// <typeparam name="TValue"></typeparam>
    public void Append<TValue>(IValueSerializer<TValue> serializer, TValue value)
    {
        var offset = _writer.Length;
        serializer.Serialize(value, _writer);
        var size = _writer.Length - offset;

        _offsets.Append((uint)offset);
        _sizes.Append((uint)size);
    }

    public void Shuffle(int[] pidxs)
    {
        _offsets.Shuffle(pidxs);
        _sizes.Shuffle(pidxs);
    }

    public void Initialize(IEnumerable<ReadOnlyMemory<byte>> select)
    {
        _offsets.Reset();
        _sizes.Reset();
        _writer.Reset();

        foreach (var value in select)
        {
            Append(value.Span);
        }
    }

    public static AppendableBlobColumn UnpackFrom(IBlobColumn indexChunkValues)
    {
        var newColumn = new AppendableBlobColumn();
        foreach (var value in indexChunkValues)
        {
            newColumn.Append(value.Span);
        }
        return newColumn;
    }

    public IEnumerator<ReadOnlyMemory<byte>> GetEnumerator()
    {
        for (var i = 0; i < Length; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
