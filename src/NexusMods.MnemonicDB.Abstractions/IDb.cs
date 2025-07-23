using System;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Represents an immutable database fixed to a specific TxId.
/// </summary>
public interface IDb : IDatomsIndex, IEquatable<IDb>
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
    /// Get all the datoms for the given entity id.
    /// </summary>
    public EntitySegment Datoms(EntityId id)
    {
        return GetEntitySegment(this, id);
    }
    
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
    /// Gets and caches all the models that point to the given entity via the given attribute.
    /// </summary>
    public Entities<TModel> GetBackrefModels<TModel>(AttributeId attribute, EntityId id)
        where TModel : IReadOnlyModel<TModel>;

    /// <summary>
    /// Gets and caches all the models that point to the given entity via the given attribute.
    /// </summary>
    public Entities<TModel> GetBackrefModels<TModel>(ReferencesAttribute attribute, EntityId id)
        where TModel : IReadOnlyModel<TModel>
    {
        var aid = AttributeCache.GetAttributeId(attribute.Id);
        return GetBackrefModels<TModel>(aid, id);
    }
    
    /// <summary>
    /// Gets and caches all the models that point to the given entity via the given attribute.
    /// </summary>
    public Entities<TModel> GetBackrefModels<TModel>(ReferenceAttribute attribute, EntityId id)
        where TModel : IReadOnlyModel<TModel>
    {
        var aid = AttributeCache.GetAttributeId(attribute.Id);
        return GetBackrefModels<TModel>(aid, id);
    }
    
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
    /// Create the next version of the database with the given result and the transaction id that the result was assigned.
    /// </summary>
    IDb WithNext(StoreResult result, TxId resultAssignedTxId);
    
    /// <summary>
    /// Gets the index segment for the given entity id.
    /// </summary>
    public EntitySegment Get(EntityId entityId)
    {
        return GetEntitySegment(this, entityId);
    }

    /// <summary>
    /// Process and store the data from the given analyzers.
    /// </summary>
    void Analyze(IDb? prev, IAnalyzer[] analyzers);

    /// <summary>
    /// Caches all the entities in the provided entity id segment. This load operation is done in bulk so it's often
    /// faster to run this method before accessing the provided models randomly.
    /// </summary>
    void BulkCache(EntityIds ids);
    
}
