﻿using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
///     Compares values and assumes that some previous comparator will guarantee that the values are of the same attribute.
/// </summary>
[UsedImplicitly]
public sealed class ValueComparer : IElementComparer
{
    
    /// <inheritdoc />
    public static unsafe int Compare(KeyPrefix* aPrefix, byte* aPtr, int aLen, KeyPrefix* bPrefix, byte* bPtr, int bLen)
    {
        return Serializer.CompareDatoms(aPrefix, aPtr, aLen, bPrefix, bPtr, bLen);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe int Compare(byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        var typeA = (*(KeyPrefix*)aPtr).ValueTag;
        var typeB = (*(KeyPrefix*)bPtr).ValueTag;
        
        var typeAByte = (byte)typeA;
        var typeBByte = (byte)typeB;
        
        if (typeAByte != typeBByte)
            return typeAByte.CompareTo(typeBByte);
        
        // Fastpath for VAET indexes
        if (typeA == ValueTag.Reference)
        {
            var aId = *(ulong*)(aPtr + KeyPrefix.Size);
            var bId = *(ulong*)(bPtr + KeyPrefix.Size);
            return aId.CompareTo(bId);
        }

        return Serializer.Compare(typeA, aPtr + KeyPrefix.Size, aLen - KeyPrefix.Size, typeB, bPtr + KeyPrefix.Size, bLen - KeyPrefix.Size);
    }

    /// <inheritdoc />
    public static int Compare(in Datom a, in Datom b)
    {
        var typeA = a.Prefix.ValueTag;
        var typeB = b.Prefix.ValueTag;
        
        if (typeA != typeB)
            return typeA.CompareTo(typeB);
        
        unsafe
        {
            fixed (byte* aPtr = a.ValueSpan)
            {
                fixed (byte* bPtr = b.ValueSpan)
                {
                    return typeA.Compare(aPtr, a.ValueSpan.Length, bPtr, b.ValueSpan.Length);
                }
            }
        }
    }

    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var typeA = KeyPrefix.Read(a).ValueTag;
        var typeB = KeyPrefix.Read(b).ValueTag;
        
        if (typeA != typeB)
            return typeA.CompareTo(typeB);

        unsafe
        {
            fixed (byte* aPtr = a.SliceFast(KeyPrefix.Size))
            {
                fixed (byte* bPtr = b.SliceFast(KeyPrefix.Size))
                {
                    return typeA.Compare(aPtr, a.Length - KeyPrefix.Size, bPtr, b.Length - KeyPrefix.Size);
                }
            }
        }
    }

   
}
