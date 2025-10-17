using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// A slice descriptor for a backreference from a given entity via a given attribute
/// </summary>
public readonly struct BackRefSlice(AttributeId aid, EntityId eid) : ISliceDescriptor
{
    /// <inheritdoc />
    public void Reset<T>(T iterator, bool useHistory) where T : ILowLevelIterator, allows ref struct
    {
        var index = useHistory ? IndexType.VAETHistory : IndexType.VAETCurrent;
        Span<byte> fullSpan = stackalloc byte[KeyPrefix.Size + sizeof(ulong)];
        var prefix = new KeyPrefix(EntityId.MinValueNoPartition, aid, TxId.MinValue, false, ValueTag.Reference, index);
        MemoryMarshal.Write(fullSpan, prefix);
        MemoryMarshal.Write(fullSpan.SliceFast(KeyPrefix.Size), eid);
        iterator.SeekTo(fullSpan);
    }
    
    /// <inheritdoc />
    public void MoveNext<T>(T iterator) where T : ILowLevelIterator, allows ref struct
    {
        iterator.Next();
    }

    /// <inheritdoc />
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool useHistory)
    {
        var index = useHistory ? IndexType.VAETHistory : IndexType.VAETCurrent;
        var prefix = KeyPrefix.Read(keySpan);
        if (prefix.A != aid || prefix.Index != index) 
            return false;
        
        var eidValue = MemoryMarshal.Read<EntityId>(keySpan.SliceFast(KeyPrefix.Size));
        return eidValue == eid;
    }


    /// <inheritdoc />
    public void Deconstruct(out Datom from, out Datom to, out bool isReversed)
    {
        var valueMemory = GC.AllocateUninitializedArray<byte>(sizeof(ulong));
        MemoryMarshal.Write(valueMemory, eid);

        from = new Datom(new KeyPrefix(EntityId.MinValueNoPartition, aid, TxId.MinValue, false, ValueTag.Reference, IndexType.VAETCurrent), valueMemory);
        to = new Datom(new KeyPrefix(EntityId.MaxValueNoPartition, aid, TxId.MaxValue, false, ValueTag.Reference, IndexType.VAETCurrent), valueMemory);
        isReversed = false;
    }    
}
