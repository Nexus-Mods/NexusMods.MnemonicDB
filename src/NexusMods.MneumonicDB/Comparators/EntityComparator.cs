using System;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Comparators;

public class EntityCacheComparator : IDatomComparator
{
    public static int Compare(IAttributeRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var keyA = MemoryMarshal.Read<KeyPrefix>(a);
        var keyB = MemoryMarshal.Read<KeyPrefix>(b);

        return keyA.A.CompareTo(keyB.A);
    }
}
