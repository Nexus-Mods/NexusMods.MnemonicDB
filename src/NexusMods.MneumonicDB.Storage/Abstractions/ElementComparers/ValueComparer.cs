using System;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB.Storage.Abstractions.ElementComparers;

/// <summary>
///     Compares values and assumes that some previous comparator will guarantee that the values are of the same attribute.
/// </summary>
public class ValueComparer : IElementComparer
{
    public static int Compare(AttributeRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var attrA = MemoryMarshal.Read<KeyPrefix>(a).A;

        unsafe
        {
            return registry.CompareValues(attrA, a.SliceFast(sizeof(KeyPrefix)), b.SliceFast(sizeof(KeyPrefix)));
        }
    }
}
