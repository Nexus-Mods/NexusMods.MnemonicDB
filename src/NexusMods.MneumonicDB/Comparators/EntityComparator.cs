using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Comparators;

public class EntityCacheComparator<TRegistry>(TRegistry registry) : IDatomComparator<TRegistry>
     where TRegistry : IAttributeRegistry
{
    public static int Compare(TRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var keyA = MemoryMarshal.Read<KeyPrefix>(a);
        var keyB = MemoryMarshal.Read<KeyPrefix>(b);

        return keyA.A.CompareTo(keyB.A);
    }

    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Compare(registry, a, b);
    }
}
