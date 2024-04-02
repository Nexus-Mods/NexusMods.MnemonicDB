﻿using System;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Storage.Abstractions.ElementComparers;

public class AComparer<TRegistry> : IElementComparer<TRegistry>
    where TRegistry : IAttributeRegistry
{
    public static int Compare(TRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<KeyPrefix>(a).A.CompareTo(MemoryMarshal.Read<KeyPrefix>(b).A);
    }
}
