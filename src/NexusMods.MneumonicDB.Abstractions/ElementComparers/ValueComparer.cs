using System;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB.Abstractions.ElementComparers;

/// <summary>
///     Compares values and assumes that some previous comparator will guarantee that the values are of the same attribute.
/// </summary>
public class ValueComparer<TRegistry> : IElementComparer<TRegistry>
    where TRegistry : IAttributeRegistry
{
    /// <inheritdoc />
    public static int Compare(TRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var attrA = MemoryMarshal.Read<KeyPrefix>(a).A;

        unsafe
        {
            return registry.CompareValues(attrA, a.SliceFast(sizeof(KeyPrefix)), b.SliceFast(sizeof(KeyPrefix)));
        }
    }
}
