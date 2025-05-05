using System;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

public interface ISegment
{
    /// <summary>
    /// Raw access to the segment data
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }
    
    /// <summary>
    /// The number of rows in the segment
    /// </summary>
    public int Count => MemoryMarshal.Read<int>(Data.Span);
}

public static class SegmentExtensions
{
    public static int GetCount(this ISegment segment) => MemoryMarshal.Read<int>(segment.Data.Span);
}

public interface ISegment<TValue1> : IHaveColumn<TValue1>
    where TValue1 : unmanaged
{
    /// <summary>
    /// Builds the segment of this type from the given builder
    /// </summary>
    public static Memory<byte> Build<T>(in T builder) where T : IIndexSegmentBuilder
    {
        return builder.Build<TValue1>();
    }
    
    /// <summary>
    /// The values of the first column
    /// </summary>
    public unsafe ReadOnlySpan<TValue1> Values1 => MemoryMarshal.Cast<byte, TValue1>(Data.Span.SliceFast(sizeof(int), Count * sizeof(TValue1)));
}

public interface ISegment<TValue1, TValue2, TValue3> : ISegment, IHaveColumn<TValue1, TValue2, TValue3>
    where TValue1 : unmanaged
    where TValue2 : unmanaged
    where TValue3 : unmanaged
{

}
