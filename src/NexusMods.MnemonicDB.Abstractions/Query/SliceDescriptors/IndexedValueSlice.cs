using System;
using System.Diagnostics;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// A slice descriptor for a given AttributeId and Value the value must be indexed
/// </summary>
public struct IndexedValueSlice : ISliceDescriptor, IDisposable
{
    private readonly PooledMemoryBufferWriter _writer;
    
    public IndexedValueSlice(AttributeId attrId, object value, AttributeCache cache)
    {
        Debug.Assert(cache.IsIndexed(attrId));
        var tag = cache.GetValueTag(attrId);
        _writer = new PooledMemoryBufferWriter(sizeof(ulong) + KeyPrefix.Size);
        var prefix = new KeyPrefix(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false, tag, IndexType.AVETCurrent);
        _writer.Write(prefix);
        tag.Write(value, _writer);
    }

    public IndexedValueSlice(PooledMemoryBufferWriter writer)
    {
        _writer = writer;
    }
    
    public IndexedValueSlice(AttributeId attrId, ValueTag tag, object value)
    {
        _writer = new PooledMemoryBufferWriter(sizeof(ulong) + KeyPrefix.Size);
        var prefix = new KeyPrefix(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false, tag, IndexType.AVETCurrent);
        _writer.Write(prefix);
        tag.Write(value, _writer);
    }
    
    [MustUseReturnValue]
    public static IndexedValueSlice Create<T>(AttributeId attrId, T value, AttributeCache cache)
    {
        Debug.Assert(cache.IsIndexed(attrId));
        var tag = cache.GetValueTag(attrId);
        var writer = new PooledMemoryBufferWriter(sizeof(ulong) + KeyPrefix.Size);
        var prefix = new KeyPrefix(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false, tag, IndexType.AVETCurrent);
        writer.Write(prefix);
        tag.Write(value, writer);
        return new IndexedValueSlice(writer);
    }
    
    
    public void Reset<T>(T iterator, bool history = false) where T : ILowLevelIterator, allows ref struct
    {
        if (history)
        {
            Span<byte> innerSpan = stackalloc byte[_writer.GetWrittenSpan().Length];
            _writer.GetWrittenSpan().CopyTo(innerSpan);
            ref var prefixRef = ref innerSpan.CastFast<byte, KeyPrefix>().DangerousGetReference();
            prefixRef = prefixRef with { Index = IndexType.AVETHistory };
            iterator.SeekTo(innerSpan);
        }
        else
        {
            iterator.SeekTo(_writer.GetWrittenSpan());
        }
    }

    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool history = false)
    {
        var index = history ? IndexType.AVETHistory : IndexType.AVETCurrent;
        var prefix = KeyPrefix.Read(keySpan);
        var thisPrefix = KeyPrefix.Read(_writer.GetWrittenSpan());
        return prefix.A == thisPrefix.A &&
               prefix.Index == index &&
               prefix.ValueTag == thisPrefix.ValueTag &&
               prefix.ValueTag.Compare(keySpan.SliceFast(KeyPrefix.Size),
                   _writer.GetWrittenSpan().SliceFast(KeyPrefix.Size)) == 0;
    }

    public bool IsTotalOrdered => false;
    public void Deconstruct(out Datom fromDatom, out Datom toDatom)
    {
        var prefix = KeyPrefix.Read(_writer.GetWrittenSpan());
        var value = prefix.ValueTag.Read<object>(_writer.GetWrittenSpan().SliceFast(KeyPrefix.Size));
        fromDatom = new Datom(new KeyPrefix(EntityId.MinValueNoPartition, prefix.A, TxId.MinValue, false, prefix.ValueTag, IndexType.AVETCurrent), value);
        toDatom = new Datom(new KeyPrefix(EntityId.MaxValueNoPartition, prefix.A, TxId.MaxValue, false, prefix.ValueTag, IndexType.AVETCurrent), value);
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
    
    /// <summary>
    /// Uncachable slice.
    /// </summary>
    public object? CacheKey => null;
}
