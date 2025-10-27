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
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool useHistory)
    {
        var index = useHistory ? IndexType.VAETHistory : IndexType.VAETCurrent;
        var prefix = KeyPrefix.Read(keySpan);
        if (prefix.A != aid || prefix.Index != index) 
            return false;
        
        var eidValue = MemoryMarshal.Read<EntityId>(keySpan.SliceFast(KeyPrefix.Size));
        return eidValue == eid;
    }

    public bool IsTotalOrdered => false;
    
    public void Deconstruct(out Datom fromDatom, out Datom toDatom)
    {
        fromDatom = new Datom(new KeyPrefix(EntityId.MinValueNoPartition, aid, TxId.MinValue, false, ValueTag.Reference, IndexType.VAETCurrent), eid);
        toDatom = new Datom(new KeyPrefix(EntityId.MaxValueNoPartition, aid, TxId.MaxValue, false, ValueTag.Reference, IndexType.VAETCurrent), eid);
    }

    /// <summary>
    /// Uncachable slice.
    /// </summary>
    public object? CacheKey => (typeof(BackRefSlice), aid, eid);
}
