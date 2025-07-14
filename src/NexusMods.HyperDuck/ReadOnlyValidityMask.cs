using System;
using System.Runtime.CompilerServices;

namespace NexusMods.HyperDuck;

public unsafe partial struct ReadOnlyValidityMask
{
    private readonly ulong* _mask;
    private readonly ulong _rowCount;
    
    internal ReadOnlyValidityMask(ulong* mask, ulong rowCount)
    {
        _mask = mask;
        _rowCount = rowCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool IsValid(ulong rowIndex)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(rowIndex, _rowCount);
        if (_mask == null)
            return true;
        
        ulong maskIndex = rowIndex / 64;
        ulong bitIndex = rowIndex % 64;
        return (_mask[maskIndex] & (1UL << (int)bitIndex)) != 0;
    }

}