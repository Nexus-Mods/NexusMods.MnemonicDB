using System;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

public interface IHaveColumn<TValue1> : ISegment
    where TValue1 : unmanaged;

public interface IHaveColumn<TValue1, TValue2> : IHaveColumn<TValue1>
    where TValue1 : unmanaged
    where TValue2 : unmanaged;

public interface IHaveColumn<TValue1, TValue2, TValue3> : IHaveColumn<TValue1, TValue2> 
    where TValue1 : unmanaged
    where TValue2 : unmanaged
    where TValue3 : unmanaged;

public static class HaveColumnExtensions
{
    /// <summary>
    /// The values of the first column
    /// </summary>
    public static unsafe ReadOnlySpan<TValue1> GetValues1<TColumn, TValue1>(this TColumn segment) 
        where TColumn : IHaveColumn<TValue1> 
        where TValue1 : unmanaged => 
        MemoryMarshal.Cast<byte, TValue1>(segment.Data.Span.SliceFast(sizeof(int), segment.Count * sizeof(TValue1)));
    
    /// <summary>
    /// Get the values of the second column
    /// </summary>
    public static unsafe ReadOnlySpan<TValue2> GetValues2<TColumn, TValue1, TValue2>(this TColumn segment) 
        where TColumn : IHaveColumn<TValue1, TValue2> 
        where TValue1 : unmanaged
        where TValue2 : unmanaged => 
        MemoryMarshal.Cast<byte, TValue2>(segment.Data.Span.SliceFast(sizeof(int) + segment.Count * sizeof(TValue1), segment.Count * sizeof(TValue2)));
    
    /// <summary>
    /// Get the values of the third column
    /// </summary>
    public static unsafe ReadOnlySpan<TValue3> GetValues3<TColumn, TValue1, TValue2, TValue3>(this TColumn segment) 
        where TColumn : IHaveColumn<TValue1, TValue2, TValue3> 
        where TValue1 : unmanaged
        where TValue2 : unmanaged
        where TValue3 : unmanaged =>
        MemoryMarshal.Cast<byte, TValue3>(segment.Data.Span.SliceFast(sizeof(int) + segment.Count * (sizeof(TValue1) + sizeof(TValue2)), segment.Count * sizeof(TValue3)));
    
}
