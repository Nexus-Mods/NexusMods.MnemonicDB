using System;
using System.Buffers;
using System.Collections.Generic;
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

    public void Swap(int idx1, int idx2)
    {
        _offsets.Swap(idx1, idx2);
        _sizes.Swap(idx1, idx2);
    }
}
