using System;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions.ElementComparers;
using NexusMods.MneumonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB.Storage.Abstractions.ElementComparers;

/// <summary>
///     Unmanaged value comparer, assumes that the values will be of the same attribute and of type T.
/// </summary>
public class UnmanagedValueComparer<T, TRegistry> : IElementComparer<TRegistry>
    where T : unmanaged, IComparable<T>
    where TRegistry : IAttributeRegistry
{
    /// <inheritdoc />
    public static int Compare(TRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
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
