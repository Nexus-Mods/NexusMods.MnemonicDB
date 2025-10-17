using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Traits;

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
    DatomList RecentlyAdded { get; }

    /// <summary>
    /// The snapshot that this database is based on.
    /// </summary>
    ISnapshot Snapshot { get; }

    /// <summary>
    /// The attribute cache for this database.
    /// </summary>
    AttributeCache AttributeCache { get; }
    
    /// <summary>
    ///     Gets the datoms for the given transaction id.
    /// </summary>
    public DatomList Datoms(TxId txId);
    
    /// <summary>
    /// Finds all datoms that have the given attribute
    /// </summary>
    DatomList Datoms(IAttribute attribute);
    
    /// <summary>
    /// Finds all the datoms that have the given attribute with the given value.
    /// </summary>
    DatomList Datoms<TValue>(IWritableAttribute<TValue> attribute, TValue value);
    
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
    /// Process and store the data from the given analyzers.
    /// </summary>
    void Analyze(IDb? prev, IAnalyzer[] analyzers);

    /// <summary>
    /// Caches all the entities in the provided entity id segment. This load operation is done in bulk so it's often
    /// faster to run this method before accessing the provided models randomly.
    /// </summary>
    void BulkCache(EntityIds ids);
    
}
