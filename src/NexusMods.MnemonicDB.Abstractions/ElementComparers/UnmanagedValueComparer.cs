using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;

/// <summary>
///     Unmanaged value comparer, assumes that the values will be of the same attribute and of type T.
/// </summary>
public class UnmanagedValueComparer<T> : IElementComparer
    where T : unmanaged, IComparable<T>
{
    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
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
