using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Algorithms;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.Columns.PackedColumns;

public class PackedBlobColumn : IBlobColumn
{
    private ReadOnlyMemory<byte> _data;
    private IColumn<uint> _offsets;
    private IColumn<uint> _sizes;

    public PackedBlobColumn(ReadOnlyMemory<byte> data, IColumn<uint> offsets, IColumn<uint> sizes)
    {
        _data = data;
        _offsets = offsets;
        _sizes = sizes;
    }

    public ReadOnlyMemory<byte> this[int idx] => _data.Slice((int)_offsets[idx], (int)_sizes[idx]);

    public int Length => _sizes.Length;
    public IBlobColumn Pack()
    {
        return this;
    }

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        writer.WriteFourCC(FourCC.PackedBlob);
        _offsets.WriteTo(writer);
        _sizes.WriteTo(writer);
        writer.Write(_data.Span.Length);
        writer.Write(_data.Span);
    }

    public static IBlobColumn ReadFrom(ref BufferReader src, int length)
    {
        var offsets = ColumnReader.ReadColumn<uint>(ref src, length);
        var sizes = ColumnReader.ReadColumn<uint>(ref src, length);
        var dataLength = src.Read<int>();
        var data = src.ReadMemory(dataLength);

        return new PackedBlobColumn(data, offsets, sizes);
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
