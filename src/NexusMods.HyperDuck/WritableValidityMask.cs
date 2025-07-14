using System;

namespace NexusMods.HyperDuck;

/// <summary>
/// A bitmask composed of 64bit ulongs, one bit per row
/// </summary>
public unsafe struct WritableValidityMask
{
    private ulong* _mask;
    private ulong _rowCount;

    public WritableValidityMask(ulong* mask, ulong rowCount)
    {
        _mask = mask;
        _rowCount = rowCount;
    }

    public bool this[ulong index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _rowCount);
            var ulongIndex = index / 64;
            var bitIndex = index % 64;
            return (_mask[ulongIndex] & (1UL << (int)bitIndex)) != 0;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _rowCount);
            var ulongIndex = index / 64;
            var bitIndex = index % 64;
            if (value)
            {
                _mask[ulongIndex] |= (1UL << (int)bitIndex);
            }
            else
            {
                _mask[ulongIndex] &= ~(1UL << (int)bitIndex);
            }
        }
    }
}