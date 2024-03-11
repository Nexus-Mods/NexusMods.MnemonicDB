using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

[StructLayout(LayoutKind.Explicit, Size = 8)]
public unsafe struct LowLevelHeader
{
    [FieldOffset(0)]
    public LowLevelType Type;

    [FieldOffset(2)]
    public int Length;

    [FieldOffset(6)]
    public LowLevelConstant Constant;

    [FieldOffset(6)]
    public LowLevelUnpacked Unpacked;

    [FieldOffset(6)]
    public LowLevelPacked Packed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int HeaderSize() => Type switch
    {
        LowLevelType.Unpacked => 4 + sizeof(LowLevelUnpacked),
        LowLevelType.Constant => 4 + sizeof(LowLevelConstant),
        LowLevelType.Packed => 4 + sizeof(LowLevelPacked),
        _ => sizeof(LowLevelPacked)
    };


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DataSize() => Type switch
    {
        LowLevelType.Unpacked => sizeof(ulong) * Length,
        LowLevelType.Constant => Length,
        LowLevelType.Packed => Length * Packed.ValueBytes * Length,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> DataSpan(Span<byte> data) => Type switch
    {
        LowLevelType.Unpacked => data.SliceFast(HeaderSize()),
        LowLevelType.Constant => data.SliceFast(HeaderSize()),
        LowLevelType.Packed => data.SliceFast(HeaderSize())
    };


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> DataSpan(ReadOnlySpan<byte> data) => Type switch
    {
        LowLevelType.Unpacked => data.SliceFast(HeaderSize()),
        LowLevelType.Constant => data.SliceFast(HeaderSize()),
        LowLevelType.Packed => data.SliceFast(HeaderSize())
    };

    public ulong Get(ReadOnlySpan<byte> span, int idx)
    {
        return Type switch
        {
            LowLevelType.Unpacked => Unpacked.Get(span.SliceFast(HeaderSize()), idx),
            LowLevelType.Constant => Constant.Get(span.SliceFast(HeaderSize()), idx),
            LowLevelType.Packed => Packed.Get(span.SliceFast(HeaderSize()), idx)
        };
    }

    public void CopyTo(ReadOnlySpan<byte> src, int offset, Span<ulong> dest)
    {
        switch (Type)
        {
            case LowLevelType.Unpacked:
                Unpacked.CopyTo(offset, dest);
                return;
            case LowLevelType.Constant:
                Constant.CopyTo(offset, dest);
                return;
            case LowLevelType.Packed:
                Packed.CopyTo(DataSpan(src), offset, dest);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(Type));
        }
    }
}
