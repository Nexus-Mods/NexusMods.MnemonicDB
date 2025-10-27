using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Query;

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
    Datoms RecentlyAdded { get; }

    /// <summary>
    /// The snapshot that this database is based on.
    /// </summary>
    ISnapshot Snapshot { get; }
    
    
    public Datoms Datoms<THighLevel, TLowLevel, TSerializer>(Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value) 
        where THighLevel : notnull 
        where TLowLevel : notnull 
        where TSerializer : IValueSerializer<TLowLevel>
    {
        using var slice = SliceDescriptor.Create(attr, value, AttributeCache);
        return Datoms(slice);
    }
    
    public Datoms Datoms(IAttribute attribute)
    {
        return Datoms(SliceDescriptor.Create(attribute, AttributeCache));
    }

    /// <summary>
    /// Gets and caches all the models that point to the given entity via the given attribute.
    /// </summary>
    public IEnumerable<TModel> GetBackrefModels<TModel>(AttributeId attribute, EntityId id)
        where TModel : IReadOnlyModel<TModel>
    {
        return this[attribute, id]
            .Select(d => TModel.Create(this, d.E));
    }

    /// <summary>
    /// Gets and caches all the models that point to the given entity via the given attribute.
    /// </summary>
    public IEnumerable<TModel> GetBackrefModels<TModel>(ReferencesAttribute attribute, EntityId id)
        where TModel : IReadOnlyModel<TModel>
    {
        var aid = AttributeCache.GetAttributeId(attribute.Id);
        return GetBackrefModels<TModel>(aid, id);
    }
    
    /// <summary>
    /// Gets and caches all the models that point to the given entity via the given attribute.
    /// </summary>
    public IEnumerable<TModel> GetBackrefModels<TModel>(ReferenceAttribute attribute, EntityId id)
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
    /// Create the next version of the database with the given result and the transaction id that the result was assigned.
    /// </summary>
    IDb WithNext(StoreResult result, TxId resultAssignedTxId);

    /// <summary>
    /// Process and store the data from the given analyzers.
    /// </summary>
    void Analyze(IDb? prev, IAnalyzer[] analyzers);

    /// <summary>
    /// Create a (temporary) database that acts like the current database, but with the given datoms transacted into
    /// it. No changes to the underlying datastore will be made, but this Db can be handed to any query functions and
    /// will perform as expected. Note: any datoms added this way will still retain their temporary ids, as the actual
    /// ids will be assigned when the transaction is comitted.
    /// </summary>
    IDb AsIf(Datoms datoms);
}
