using System;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Storage.Abstractions.ElementComparers;

public class EComparer : IElementComparer
{
    public static int Compare(AttributeRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<KeyPrefix>(a).E.CompareTo(MemoryMarshal.Read<KeyPrefix>(b).E);
    }
}
