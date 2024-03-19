using System;
using System.Runtime.CompilerServices;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

public partial class BlobColumn
{
    private bool _isFrozen = true;

    public bool IsFrozen => _isFrozen;

    public BlobColumn(bool isFrozen) : this()
    {
        _isFrozen = isFrozen;
    }

    public static BlobColumn Create(bool isFrozen = false)
    {
        return new BlobColumn(isFrozen)
        {
            Count = 0,
            Offsets = ULongColumn.Create(),
            Lengths = ULongColumn.Create(),
            Data = new Memory<byte>(new byte[1024])
        };
    }


    public BlobColumn Freeze()
    {
        if (_isFrozen)
        {
            return this;
        }

        Offsets = Offsets.Freeze();
        Lengths = Lengths.Freeze();
        Data = Data[..UsedDataLength];
        _isFrozen = true;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlobColumn NotFrozen()
    {
        return !_isFrozen ? this : Thaw();
    }

    public BlobColumn Thaw()
    {
        var newData = new byte[Data.Length];
        Data.CopyTo(newData);
        return new BlobColumn(false)
        {
            Lengths = Lengths.Thaw(),
            Offsets = Offsets.Thaw(),
            Data = newData
        };
    }

    public ReadOnlySpan<byte> this[int offset]
    {
        get
        {
            var start = Offsets[offset];
            var end = Lengths[offset];
            return Data.Slice((int)start, (int)end).Span;
        }
    }

    public ReadOnlyMemory<byte> Memory => Data;

    public ReadOnlyMemory<byte> GetMemory(int offset)
    {
        var start = Offsets[offset];
        var end = Lengths[offset];
        return Data.Slice((int)start, (int)end);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureThawed()
    {
        if (_isFrozen)
        {
            throw new InvalidOperationException("Column is frozen");
        }
    }

    /// <summary>
    /// Ensure that the column has enough space to add a element of size i
    /// </summary>
    private void Ensure(int size)
    {
        if (UsedDataLength + size <= Data.Length)
        {
            return;
        }

        var newData = new byte[Data.Length * 2];
        Data.Span.CopyTo(newData);
        Data = newData;
    }

    public void Add(byte[] value)
    {
        Add(value.AsSpan());
    }

    /// <summary>
    /// Add all the valid values from the chunk to the column
    /// </summary>
    public void Add(in DatomChunk chunk)
    {
        for (var idx = 0; idx < DatomChunk.ChunkSize; idx++)
        {
            if (chunk.IsValid(idx))
            {
                Add(chunk.GetValue(idx));
            }
        }
    }

    public void Add(ReadOnlySpan<byte> value)
    {
        EnsureThawed();
        Ensure(value.Length);
        value.CopyTo(Data.Span.SliceFast(UsedDataLength));
        Offsets.Add((ulong)UsedDataLength);
        Lengths.Add((ulong)value.Length);
        Count++;
        UsedDataLength += value.Length;
    }
}
