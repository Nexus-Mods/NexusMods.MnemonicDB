using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.MnemonicDB.Abstractions.ValueSerializers;

/// <summary>
/// A value serializer for an unmanaged, un-remappable value type, handling all but the ValueTag property
/// </summary>
public interface IUnmanagedValueSerializer<T> : IValueSerializer<T> 
    where T : struct, IComparable<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static T IValueSerializer<T>.Read(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<T>(span);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void IValueSerializer<T>.Write<TWriter>(T value, TWriter writer)
    {
        var span = writer.GetSpan(Marshal.SizeOf<T>());
        MemoryMarshal.Write(span, value);
        writer.Advance(Marshal.SizeOf<T>());
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void IValueSerializer<T>.Remap(Span<byte> span, Func<EntityId, EntityId> remapFn)
    {
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int IValueSerializer.Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var aVal = MemoryMarshal.Read<T>(a);
        var bVal = MemoryMarshal.Read<T>(b);
        return aVal.CompareTo(bVal);
    }
}
