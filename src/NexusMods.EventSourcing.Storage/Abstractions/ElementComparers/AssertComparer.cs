using System;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Abstractions.ElementComparers;

public class AssertComparer : IElementComparer
{
    public static int Compare(AttributeRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<KeyPrefix>(a).IsRetract.CompareTo(MemoryMarshal.Read<KeyPrefix>(b).IsRetract);
    }
}
