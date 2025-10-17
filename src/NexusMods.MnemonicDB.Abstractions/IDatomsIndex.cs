using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB.Abstractions;

public interface IDatomsIndex
{
    public AttributeCache AttributeCache { get; }
    
    /// <summary>
    /// Get all the datoms for the given entity id. 
    /// </summary>
    public DatomList this[EntityId e] { get; }
    
    /// <summary>
    /// Get all the datoms for the given attribute id.
    /// </summary>
    /// <param name="a"></param>
    public DatomList this[AttributeId a] { get; }
    
    /// <summary>
    /// Get all the datoms for the given transaction id.
    /// </summary>
    /// <param name="tx"></param>
    public DatomList this[TxId tx] { get; }
    
    /// <summary>
    /// Get all the datoms that are references to the given entity id, via the given attribute.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="a"></param>
    public DatomList this[AttributeId a, EntityId e] { get; }
    
    
    /// <summary>
    /// Get the datoms for a specific descriptor
    /// </summary>
    public DatomList Datoms<TDescriptor>(TDescriptor descriptor) where TDescriptor : ISliceDescriptor;

    /// <summary>
    /// Return a chunked sequence of datoms for a specific descriptor, chunks will be of the specified size
    /// except for the last chunk which may be smaller
    /// </summary>
    public IEnumerable<DatomList> DatomsChunked<TSliceDescriptor>(TSliceDescriptor descriptor, int chunkSize)
        where TSliceDescriptor : ISliceDescriptor;
    
    /// <summary>
    /// A lightweight datom segment doesn't load the entire set into memory.
    /// </summary>
    public ILightweightDatomSegment LightweightDatoms<TDescriptor>(TDescriptor descriptor, bool totalOrdered = false)
        where TDescriptor : ISliceDescriptor;

    /// <summary>
    /// Returns an index segment of all the datoms that are a reference pointing to the given entity id.
    /// </summary>
    DatomList ReferencesTo(EntityId eid);

    /// <summary>
    /// Loads sorted chunks of ids (of the given size) for the given attribute.  
    /// </summary>
    public int IdsForPrimaryAttribute(AttributeId attributeId, int chunkSize, out List<EntityId[]> chunks);

}
