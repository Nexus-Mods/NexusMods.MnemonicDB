using System;
using System.Runtime.CompilerServices;

namespace NexusMods.MnemonicDB.ManagedTree.Abstractions;

public interface ISpanComparer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static abstract int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y);
}
