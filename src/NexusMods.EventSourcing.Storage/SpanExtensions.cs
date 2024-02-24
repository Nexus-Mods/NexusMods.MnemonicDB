using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage;

public static class SpanExtensions
{
    public static ReadOnlySpan<byte> CopyToList<TValue>(this ReadOnlySpan<byte> src, List<TValue> list, uint count)
        where TValue : struct
    {
        unsafe
        {
            var readSize = sizeof(TValue) * count;
            var listSpan = MemoryMarshal.Cast<byte, TValue>(src).SliceFast(0, (int)count);
            list.Clear();
            list.AddRange(listSpan);
            return src.SliceFast((int)readSize);
        }
    }

    public static ReadOnlySpan<byte> CopyToLists<TValueA, TValueB>(this ReadOnlySpan<byte> src,
        List<TValueA> listA,
        List<TValueB> listB, uint count)
        where TValueA : struct
        where TValueB : struct
    {
        var exitSpan = src.CopyToList(listA, count);
        exitSpan = exitSpan.CopyToList(listB, count);
        return exitSpan;
    }

    public static ReadOnlySpan<byte> CopyToLists<TValueA, TValueB, TValueC>(this ReadOnlySpan<byte> src,
        List<TValueA> listA,
        List<TValueB> listB,
        List<TValueC> listC,
        uint count)
        where TValueA : struct
        where TValueB : struct
        where TValueC : struct
    {
        var exitSpan = src.CopyToList(listA, count);
        exitSpan = exitSpan.CopyToList(listB, count);
        exitSpan = exitSpan.CopyToList(listC, count);
        return exitSpan;
    }

    public static ReadOnlySpan<byte> CopyToLists<TValueA, TValueB, TValueC, TValueD>
    (this ReadOnlySpan<byte> src,
        List<TValueA> listA,
        List<TValueB> listB,
        List<TValueC> listC,
        List<TValueD> listD,
        uint count)
        where TValueA : struct
        where TValueB : struct
        where TValueC : struct
        where TValueD : struct
    {
        var exitSpan = src.CopyToList(listA, count);
        exitSpan = exitSpan.CopyToList(listB, count);
        exitSpan = exitSpan.CopyToList(listC, count);
        exitSpan = exitSpan.CopyToList(listD, count);
        return exitSpan;
    }

    public static ReadOnlySpan<byte> CopyToLists<TValueA, TValueB, TValueC, TValueD, TValueE>
    (this ReadOnlySpan<byte> src,
        List<TValueA> listA,
        List<TValueB> listB,
        List<TValueC> listC,
        List<TValueD> listD,
        List<TValueE> listE,
        uint count)
        where TValueA : struct
        where TValueB : struct
        where TValueC : struct
        where TValueD : struct
        where TValueE : struct
    {
        var exitSpan = src.CopyToList(listA, count);
        exitSpan = exitSpan.CopyToList(listB, count);
        exitSpan = exitSpan.CopyToList(listC, count);
        exitSpan = exitSpan.CopyToList(listD, count);
        exitSpan = exitSpan.CopyToList(listE, count);
        return exitSpan;
    }

    public static void WriteList<TWriter, TItem>(this TWriter writer, List<TItem> lst)
    where TWriter : IBufferWriter<byte>
        where TItem : struct
    {
        unsafe
        {
            var size = lst.Count * sizeof(TItem);
            var span = writer.GetSpan(size);
            MemoryMarshal.Cast<TItem, byte>(CollectionsMarshal.AsSpan(lst).SliceFast(0, lst.Count))
                .CopyTo(span);
            writer.Advance(size);

        }

    }
}
