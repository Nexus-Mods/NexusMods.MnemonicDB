using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Comparators;

public class EntityCacheComparator<TRegistry>(TRegistry registry) : IDatomComparator
     where TRegistry : IAttributeRegistry
{
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var keyA = MemoryMarshal.Read<KeyPrefix>(a);
        var keyB = MemoryMarshal.Read<KeyPrefix>(b);

        return keyA.A.CompareTo(keyB.A);
    }

    public int CompareInstance(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return Compare(a, b);
    }
}
