using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.IndexSegments.SegmentParts;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Caching;

namespace NexusMods.MnemonicDB;

/// <summary>
/// A wrapper for a datoms index that caches several index segment types
/// </summary>
public abstract class ACachingDatomsIndex<TRefEnumerator>(AttributeCache attributeCache) : 
    ADatomsIndex<TRefEnumerator>(attributeCache)
    where TRefEnumerator : IRefDatomEnumerator
{
    private class EntityCacheStrategy(ACachingDatomsIndex<TRefEnumerator> parent) : CacheStrategy<EntityId, AVSegment>
    {
        public override Memory<byte> GetBytes(EntityId key, IDb db)
        {
            var builder = new IndexSegmentBuilder(db.AttributeCache);
            using var iterator = parent.GetRefDatomEnumerator();
            builder.AddRange(iterator, SliceDescriptor.Create(key));
            return builder.Build<AttributeIdPart, ValueTypePart, ValuePart>();
        }

        public override AVSegment GetValue(EntityId key, IDb db, Memory<byte> bytes)
        {
            return new AVSegment { Data = bytes };
        }

        public override IEnumerable<EntityId> GetKeysFromRecentlyAdded(IndexSegment segment)
        {
            throw new NotImplementedException();
        }
    }
    
}
