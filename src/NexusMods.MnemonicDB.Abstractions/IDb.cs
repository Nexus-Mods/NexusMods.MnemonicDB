using System;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Represents an immutable database fixed to a specific TxId.
/// </summary>
public interface IDb : IEquatable<IDb>
{
    /// <summary>
    ///     Gets the basis TxId of the database.
    /// </summary>
    TxId BasisTxId { get; }

    /// <summary>
    ///     The connection that this database is using for its state.
    /// </summary>
    IConnection Connection { get; set; }

    /// <summary>
    ///     The datoms that were added in the most recent transaction (indicated by the basis TxId).
    /// </summary>
    IndexSegment RecentlyAdded { get; }

    /// <summary>
    /// The snapshot that this database is based on.
    /// </summary>
    ISnapshot Snapshot { get; }

    /// <summary>
    /// The attribute cache for this database.
    /// </summary>
    AttributeCache AttributeCache { get; }
    
    /// <summary>
    /// Gets the index segment for the given entity id.
    /// </summary>
    public EntitySegment Get(EntityId entityId);

    /// <summary>
    /// Get all the datoms for the given entity id.
    /// </summary>
    public EntitySegment Datoms(EntityId id);
    
    /// <summary>
    /// Get all the datoms defined by the given slice descriptor.
    /// </summary>
    public IndexSegment Datoms<TDescriptor>(TDescriptor descriptor) where TDescriptor : ISliceDescriptor;

    /// <summary>
    /// Get all the datoms for the given slice descriptor.
    /// </summary>
    public IndexSegment Datoms(SliceDescriptor sliceDescriptor);

    /// <summary>
    ///     Gets the datoms for the given transaction id.
    /// </summary>
    public IndexSegment Datoms(TxId txId);
    
    /// <summary>
    /// Finds all datoms that have the given attribute
    /// </summary>
    IndexSegment Datoms(IAttribute attribute);
    
    /// <summary>
    /// Finds all the datoms that have the given attribute with the given value.
    /// </summary>
    IndexSegment Datoms<TValue>(IWritableAttribute<TValue> attribute, TValue value);
    
    /// <summary>
    /// Gets all the back references for this entity that are through the given attribute.
    /// </summary>
    EntityIds GetBackRefs(ReferenceAttribute attribute, EntityId id);

    /// <summary>
    /// Returns an index segment of all the datoms that are a reference pointing to the given entity id.
    /// </summary>
    IndexSegment ReferencesTo(EntityId eid);
    
    /// <summary>
    /// Get the cached data for the given analyzer.
    /// </summary>
    TReturn AnalyzerData<TAnalyzer, TReturn>() 
        where TAnalyzer : IAnalyzer<TReturn>;
    
    /// <summary>
    /// Clears the internal cache of the database.
    /// </summary>
    void ClearIndexCache();

    /// <summary>
    /// Starts a thread that begins precaching all the entities and reverse references into this database instance.
    /// </summary>
    Task PrecacheAll();

    /// <summary>
    /// Create the next version of the database with the given result and the transaction id that the result was assigned.
    /// </summary>
    IDb WithNext(StoreResult result, TxId resultAssignedTxId);

    /// <summary>
    /// Add the given analyzer data to the analyzer cache.
    /// </summary>
    void AddAnalyzerData(Type getType, object result);
}
