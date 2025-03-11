using System;

namespace NexusMods.MnemonicDB.Abstractions;

public readonly unsafe struct Ptr
{
    public readonly byte* Base;
    public readonly int Length;

    public Ptr(byte* basePtr, int length)
    {
        Base = basePtr;
        Length = length;
    }
    
    /// <summary>
    /// Get the span of the pointer.
    /// </summary>
    public ReadOnlySpan<byte> Span => new(Base, Length);

    /// <summary>
    /// Slice the pointer from the given start index, without checking bounds.
    /// </summary>
    public Ptr SliceFast(int fromStart)
    {
        return new(Base + fromStart, Length - fromStart);
    }

    /// <summary>
    /// Read a value from the pointer at the given byte offset.
    /// </summary>
    /// <returns></returns>
    public T Read<T>(int byteOffset) where T : unmanaged
    {
        return *(T*)(Base + byteOffset);
    }
}
