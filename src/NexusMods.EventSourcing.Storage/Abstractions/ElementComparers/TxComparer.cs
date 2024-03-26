﻿using System;
using System.Runtime.InteropServices;

namespace NexusMods.EventSourcing.Storage.Abstractions.ElementComparers;

public class TxComparer : IElementComparer
{
    public static int Compare(AttributeRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return MemoryMarshal.Read<KeyPrefix>(a).T.CompareTo(MemoryMarshal.Read<KeyPrefix>(b).T);
    }
}