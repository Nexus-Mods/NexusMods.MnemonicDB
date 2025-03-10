using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions;

public interface IDatomsIndex
{
    public AttributeCache AttributeCache { get; }
    
    /// <summary>
    /// Get the datoms for a specific descriptor
    /// </summary>
    public IndexSegment Datoms<TDescriptor>(TDescriptor descriptor) where TDescriptor : ISliceDescriptor;

    /// <summary>
    /// Return a chunked sequence of datoms for a specific descriptor, chunks will be of the specified size
    /// except for the last chunk which may be smaller
    /// </summary>
    public IEnumerable<IndexSegment> DatomsChunked<TSliceDescriptor>(TSliceDescriptor descriptor, int chunkSize)
        where TSliceDescriptor : ISliceDescriptor;
    
    /// <summary>
    /// Get the entity segment for a specific entity id
    /// </summary>
    public EntitySegment GetEntitySegment(IDb db, EntityId entityId);
    
    /// <summary>
    /// Gets all the entity ids pointing to the given entity id via the given attribute.
    /// </summary>
    public EntityIds GetEntityIdsPointingTo(AttributeId attribute, EntityId entityId);

    /// <summary>
    /// Gets all the entity ids pointing to the given entity id via the given attribute.
    /// </summary>
    public EntityIds GetEntityIdsPointingTo(IAttribute attribute, EntityId entityId)
    {
        var attrId = AttributeCache.GetAttributeId(attribute.Id);
        return GetEntityIdsPointingTo(attrId, entityId);
    }
    
    /// <summary>
    /// Gets all the back references for this entity that are through the given attribute.
    /// </summary>
    EntityIds GetBackRefs(ReferenceAttribute attribute, EntityId id);

    /// <summary>
    /// Returns an index segment of all the datoms that are a reference pointing to the given entity id.
    /// </summary>
    IndexSegment ReferencesTo(EntityId eid);

}
