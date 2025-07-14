using System;
using System.Runtime.CompilerServices;

namespace NexusMods.HyperDuck.Adaptor;

public static class Helpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T GetFromSpan<T>(ReadOnlySpan<T> span, ulong index)
    {
        return span[(int)index];
    }
    
    public static Type ScalarMapping(DuckDbType type)
    {
        return type switch
        {
            DuckDbType.Integer => typeof(int),
            DuckDbType.BigInt => typeof(long),
            DuckDbType.Varchar => typeof(StringElement),
            DuckDbType.List => typeof(ListEntry),
            _ => throw new NotImplementedException($"Row scalar mapping for {type} not implemented.")
        };
    }
}