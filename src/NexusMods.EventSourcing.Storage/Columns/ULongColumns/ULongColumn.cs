using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// A column backed by a FlatBuffer
/// </summary>
public partial class ULongColumn : IEnumerable<ulong>
{
    private readonly bool _isFrozen = true;

    public bool IsFrozen => _isFrozen;

    private ULongColumn(bool isFrozen) : this()
    {
        _isFrozen = isFrozen;
    }

    public static ULongColumn Create(int initialSize = 32)
    {
        return new ULongColumn(false)
        {
            Length = 0,
            Header = new UL_Column_Union(new UL_Unpacked()),
            Data = new Memory<byte>(new byte[initialSize * sizeof(ulong)])
        };
    }

    public ULongColumn Freeze()
    {
        if (_isFrozen)
        {
            return this;
        }

        var span = Data.Span.CastFast<byte, ulong>();
        var stats = Statistics.Create(span);
        var packed = stats.Pack(span);
        return packed;
    }

    /// <summary>
    /// Ensure the column is not frozen, and if it is, unfreeze it
    /// </summary>
    public ULongColumn NotFrozen()
    {
        return !_isFrozen ? this : Thaw();
    }

    /// <summary>
    /// Unfreezes the column, allowing for modifications
    /// </summary>
    public ULongColumn Thaw()
    {
        var memory = new Memory<byte>(new byte[Length * sizeof(ulong)]);
        CopyTo(0, memory.Span.CastFast<byte, ulong>());
        return new ULongColumn(false)
        {
            Length = Length,
            Header = new UL_Column_Union(new UL_Unpacked()),
            Data = memory
        };
    }

    public ulong this[int idx]
    {
        get
        {
            switch (Header.Kind)
            {
                case UL_Column_Union.ItemKind.Constant:
                    return Header.Constant.Value;
                case UL_Column_Union.ItemKind.Unpacked:
                    return Data.Span.Cast<byte, ulong>()[idx];
                case UL_Column_Union.ItemKind.Packed:
                    var header = Header.Packed;
                    var span = Data.Span;
                    var bytesMask = (1UL << (header.ValueBytes * 8)) - 1;

                    var offset = idx * header.ValueBytes;
                    var valAndPartition = MemoryMarshal.Read<ulong>(span.SliceFast(offset, 8)) & bytesMask;
                    var value = (valAndPartition >> header.PartitionBits) + header.ValueOffset;
                    var partition = (valAndPartition & ((1UL << header.PartitionBits) - 1)) + header.PartitionOffset;
                    return (partition << (8 * 7)) | value;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public ULongColumn Set(int idx, ulong value)
    {
        var column = NotFrozen();
        column.Data.Span.Cast<byte, ulong>()[idx] = value;
        return column;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureNotFrozen()
    {
        if (_isFrozen)
        {
            throw new InvalidOperationException("Column is frozen");
        }
    }

    private void Ensure(int i)
    {
        Debug.Assert(!_isFrozen);
        if (Length + i <= Data.Length / sizeof(ulong)) return;
        var newData = new Memory<byte>(new byte[Data.Length * 2]);
        Data.Span.CopyTo(newData.Span);
        Data = newData;
    }

    public ULongColumn Add(ulong value)
    {
        EnsureNotFrozen();
        Ensure(1);
        Data.Span.CastFast<byte, ulong>()[Length] = value;
        Length += 1;
        return this;
    }

    public void Add(ReadOnlySpan<ulong> values)
    {
        EnsureNotFrozen();
        Ensure(values.Length);
        values.CopyTo(Data.Span.CastFast<byte, ulong>().SliceFast(Length));
        Length += values.Length;
    }

    public void Add(IEnumerable<ulong> values)
    {
        EnsureNotFrozen();
        foreach (var value in values)
        {
            Add(value);
        }
    }

    public void Add(params ulong[] values)
    {
        EnsureNotFrozen();
        Add(values.AsSpan());
    }

    public void Add(ReadOnlySpan<ulong> values, ReadOnlySpan<ulong> mask)
    {
        EnsureNotFrozen();

        for(var i = 0; i < mask.Length; i++)
        {
            if (mask[i] == ulong.MaxValue)
            {
                Ensure(64);
                values.SliceFast(i * 64, 64).CopyTo(Data.Span.CastFast<byte, ulong>().SliceFast(Length));
                Length += 64;
            }
            else if (mask[i] != 0)
            {
                for (var j = 0; j < 64; j++)
                {
                    if ((mask[i] & (1UL << j)) == 0)
                        continue;

                    Ensure(1);
                    Data.Span.CastFast<byte, ulong>()[Length] = values[i * 64 + j];
                    Length += 1;
                }
            }
        }
    }

    public void CopyTo(int offset, Span<ulong> dest)
    {
        switch (Header.Kind)
        {
            case UL_Column_Union.ItemKind.Constant:
                dest.Fill(Header.Constant.Value);
                break;
            case UL_Column_Union.ItemKind.Unpacked:
                var srcSpan = Data.Span.Cast<byte, ulong>().SliceFast(offset, dest.Length);
                srcSpan.CopyTo(dest);
                break;
            case UL_Column_Union.ItemKind.Packed:
                var src = Data.Span;
                var header = Header.Packed;
                for (var idx = 0; idx < dest.Length; idx += 1)
                {
                    var span = src.SliceFast((idx + offset) * header.ValueBytes);
                    var valAndPartition = MemoryMarshal.Read<ulong>(span) & ((1UL << (header.ValueBytes * 8)) - 1);
                    var value = (valAndPartition >> header.PartitionBits) + header.ValueOffset;
                    var partition = (valAndPartition & ((1UL << header.PartitionBits) - 1)) + header.PartitionOffset;
                    dest[idx] = (partition << (8 * 7)) | value;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public IEnumerator<ulong> GetEnumerator()
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

