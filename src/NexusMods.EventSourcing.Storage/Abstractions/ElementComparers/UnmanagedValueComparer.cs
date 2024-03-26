using System;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Abstractions.ElementComparers;

/// <summary>
/// Unmanaged value comparer, assumes that the values will be of the same attribute and of type T.
/// </summary>
/// <typeparam name="T"></typeparam>
public class UnmanagedValueComparer<T> : IElementComparer
    where T : unmanaged, IComparable<T>
{
    public static int Compare(AttributeRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        unsafe
        {
            if (a.Length < sizeof(KeyPrefix) || b.Length < sizeof(KeyPrefix))
                return a.Length.CompareTo(b.Length);
            return MemoryMarshal.Read<T>(a.SliceFast(sizeof(KeyPrefix)))
                .CompareTo(MemoryMarshal.Read<T>(b.SliceFast(sizeof(KeyPrefix))));
        }
    }
}
