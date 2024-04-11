using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
///     Compares values and assumes that some previous comparator will guarantee that the values are of the same attribute.
/// </summary>
public class ValueComparer : IElementComparer
{
    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var aPrefix = MemoryMarshal.Read<KeyPrefix>(a);
        var bPrefix = MemoryMarshal.Read<KeyPrefix>(b);

        if (aPrefix.LowLevelType != bPrefix.LowLevelType)
            return (byte)aPrefix.LowLevelType.CompareTo(bPrefix.LowLevelType);

        return aPrefix.LowLevelType switch
        {
            LowLevelTypes.UInt => CompareUInt(in aPrefix, a.SliceFast(KeyPrefix.Size), in bPrefix,
                b.SliceFast(KeyPrefix.Size)),
            LowLevelTypes.Utf8 => a.SliceFast(KeyPrefix.Size).SequenceCompareTo(b.SliceFast(KeyPrefix.Size)),
            LowLevelTypes.Ascii => a.SliceFast(KeyPrefix.Size).SequenceCompareTo(b.SliceFast(KeyPrefix.Size)),
            _ => throw new NotSupportedException()
        };
    }

    private static int CompareUInt(in KeyPrefix aPrefix, ReadOnlySpan<byte> a, in KeyPrefix bPrefix, ReadOnlySpan<byte> b)
    {
        if (aPrefix.ValueLength != bPrefix.ValueLength)
            return aPrefix.ValueLength.CompareTo(bPrefix.ValueLength);

        return aPrefix.ValueLength switch
        {
            1 => a[0].CompareTo(b[0]),
            2 => MemoryMarshal.Read<ushort>(a).CompareTo(MemoryMarshal.Read<ushort>(b)),
            4 => MemoryMarshal.Read<uint>(a).CompareTo(MemoryMarshal.Read<uint>(b)),
            8 => MemoryMarshal.Read<ulong>(a).CompareTo(MemoryMarshal.Read<ulong>(b)),
            16 => MemoryMarshal.Read<UInt128>(a).CompareTo(MemoryMarshal.Read<UInt128>(b)),
            _ => throw new NotSupportedException()
        };
    }
}
