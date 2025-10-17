using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

public readonly unsafe struct UnmanagedValueLookupSlice<T>(AttributeId attrId, ValueTag tag, T value) : ISliceDescriptor
    where T : unmanaged
{
    public void Reset<TIterator>(TIterator iterator, bool history = false) 
        where TIterator : ILowLevelIterator, allows ref struct
    {
        Span<byte> buffer = stackalloc byte[KeyPrefix.Size + sizeof(T)];
        MemoryMarshal.Write(buffer,
            new KeyPrefix(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false, tag,
                history ? IndexType.AVETHistory : IndexType.AVETCurrent));
        MemoryMarshal.Write(buffer.SliceFast(KeyPrefix.Size), value);
        
        iterator.SeekTo(buffer);
    }

    public void MoveNext<T1>(T1 iterator) where T1 : ILowLevelIterator, allows ref struct
    {
        iterator.Next();
    }

    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool history = false)
    {
        var index = history ? IndexType.AVETHistory : IndexType.AVETCurrent;
        var prefix = KeyPrefix.Read(keySpan);
        if (prefix.Index == index && prefix.A == attrId && prefix.ValueTag == tag)
        {
            Span<byte> buffer = stackalloc byte[sizeof(T)];
            MemoryMarshal.Write(buffer, value);
            fixed (byte* ptr = keySpan.SliceFast(KeyPrefix.Size))
            fixed (byte* thisPtr = buffer)
            {
                return ValueComparer.Compare(thisPtr, sizeof(T), ptr, sizeof(T)) == 0;

            }
        }
        return false;
    }

    public void Deconstruct(out Datom from, out Datom to, out bool isReversed)
    {
        var array = GC.AllocateUninitializedArray<byte>(sizeof(T));
        MemoryMarshal.Write(array, value);
        from = new Datom(new KeyPrefix(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false, ValueTag.Null, IndexType.AVETCurrent), array);
        to = new Datom(new KeyPrefix(EntityId.MaxValueNoPartition, attrId, TxId.MaxValue, false, ValueTag.Null, IndexType.AVETCurrent), array);
        isReversed = false;
    }
}
