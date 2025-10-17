using System;
using System.Buffers;
using System.Collections.Generic;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB;

public abstract class ADatomsIndex<TRefEnumerator> : IDatomsIndex, IRefDatomEnumeratorFactory<TRefEnumerator>
    where TRefEnumerator : IRefDatomEnumerator
{
    protected ADatomsIndex(AttributeCache cache)
    {
        AttributeCache = cache;
    }
    public AttributeCache AttributeCache { get; }

    private Datoms Load<TSlice>(TSlice slice)
       where TSlice : ISliceDescriptor, allows ref struct
    {
        using var en = GetRefDatomEnumerator();
        return Abstractions.Datoms.Create(en, slice, AttributeCache);
    }
    
    public Datoms Datoms<TSlice>(TSlice slice) where TSlice : ISliceDescriptor 
        => Load(slice);

    /// <inheritdoc />
    public Datoms this[EntityId e] => Load(SliceDescriptor.Create(e));

    /// <inheritdoc />
    public Datoms this[AttributeId a] => Load(SliceDescriptor.Create(a));

    /// <inheritdoc />
    public Datoms this[TxId t] => Load(SliceDescriptor.Create(t));
    
    /// <inheritdoc />
    public Datoms this[AttributeId a, EntityId e] => Load(SliceDescriptor.Create(a, e));

    public IEnumerable<Datoms> DatomsChunked<TSliceDescriptor>(TSliceDescriptor descriptor, int chunkSize) 
        where TSliceDescriptor : ISliceDescriptor
    {
        using var iterator = GetRefDatomEnumerator();
        var currentResult = new Datoms(AttributeCache);
        while (iterator.MoveNext(descriptor))
        {
            currentResult.Add(ValueDatom.Create(iterator));
            if (currentResult.Count == chunkSize)
            {
                yield return currentResult;
                currentResult = new(AttributeCache);
            }
        }

        if (currentResult.Count > 0)
            yield return currentResult;
    }

    /// <summary>
    /// A lightweight datom segment doesn't load the entire set into memory.
    /// </summary>
    [MustDisposeResource]
    public ILightweightDatomSegment LightweightDatoms<TDescriptor>(TDescriptor descriptor, bool totalOrdered = false)
        where TDescriptor : ISliceDescriptor
    {
        var enumerator = GetRefDatomEnumerator(totalOrdered);
        return new LightweightDatomSegment<TRefEnumerator, TDescriptor>(enumerator, descriptor);
    }

    public int IdsForPrimaryAttribute(AttributeId attributeId, int chunkSize, out List<EntityId[]> chunks)
    {
        List<EntityId[]> result = [];
        using var iterator = GetRefDatomEnumerator();
        var slice = SliceDescriptor.Create(attributeId);
        var chunk = ArrayPool<EntityId>.Shared.Rent(chunkSize);
        result.Add(chunk);
        int chunkOffset = 0;
        while (iterator.MoveNext(slice))
        {
            chunk[chunkOffset] = iterator.E;
            chunkOffset++;
            if (chunkOffset >= chunk.Length)
            {
                chunk = ArrayPool<EntityId>.Shared.Rent(chunkSize);
                chunkOffset = 0;
                result.Add(chunk);
            }
        }

        chunks = result;
        return (result.Count - 1) * chunkSize + chunkOffset;
    }
    
    public Datoms ReferencesTo(EntityId eid)
    {
        return Load(SliceDescriptor.CreateReferenceTo(eid));
    }
    
    /// <inheritdoc />
    public abstract TRefEnumerator GetRefDatomEnumerator(bool totalOrder = false);
}
