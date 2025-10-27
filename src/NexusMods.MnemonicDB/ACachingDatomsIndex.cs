using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Caching;

namespace NexusMods.MnemonicDB;

/// <summary>
/// A wrapper for a datoms index that caches several index segment types
/// </summary>
public abstract class ACachingDatomsIndex<TRefEnumerator> : 
    ADatomsIndex<TRefEnumerator>
    where TRefEnumerator : IRefDatomEnumerator
{
    private CacheRoot _root;
    private readonly CacheStrategy _strategy;
    
    protected ACachingDatomsIndex(ACachingDatomsIndex<TRefEnumerator> other, Datoms addedDatoms) : base(other.AttributeCache)
    {
        _root = other._root.Evict(addedDatoms);
        _strategy = other._strategy;
    }

    protected override Datoms Load<TSlice>(TSlice slice)
    {
        var key = slice.CacheKey;
        if (key is null)
            return base.Load(slice);
        
        if (_root.Cache.TryGetValue(key, out var value))
        {
            value.Hit();
            return value.Segment; 
        }
        
        return LoadAndCache(key, slice);
    }

    private Datoms LoadAndCache<TSlice>(object key, TSlice slice) 
        where TSlice : ISliceDescriptor, allows ref struct
    {
        var datoms = base.Load(slice);
        return AddValue(key, datoms);
    }
    
    private Datoms AddValue(object key, Datoms datoms)
    {
        while (true)
        {
            var oldRoot = _root;
            
            // If the cache somehow now contains the key, return the value.
            if (oldRoot.Cache.TryGetValue(key, out var newlyAdded))
                return newlyAdded.Segment;
            
            // Otherwise, add the new value to the cache.
            var newRoot = oldRoot.With(key, datoms).Evict(_strategy.MaxTotalCount, _strategy.MaxEntries, _strategy.EvictPercentage);
            var result = Interlocked.CompareExchange(ref _root, newRoot, oldRoot);
            if (ReferenceEquals(result, oldRoot))
                return datoms;
        }
    }


    /// <summary>
    /// A wrapper for a datoms index that caches several index segment types
    /// </summary>
    protected ACachingDatomsIndex(AttributeCache attributeCache) : base(attributeCache)
    {
        _root = CacheRoot.Create();
        _strategy = new CacheStrategy();
    }
}
