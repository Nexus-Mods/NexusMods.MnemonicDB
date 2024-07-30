using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
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
    IConnection Connection { get; }
    
    /// <summary>
    ///     The datoms that were added in the most recent transaction (indicated by the basis TxId).
    /// </summary>
    IndexSegment RecentlyAdded { get; }

    /// <summary>
    /// The snapshot that this database is based on.
    /// </summary>
    ISnapshot Snapshot { get; }

    /// <summary>
    /// The registry that this database is based on.
    /// </summary>
    IAttributeRegistry Registry { get; }

    /// <summary>
    /// Gets the index segment for the given entity id.
    /// </summary>
    public IndexSegment Get(EntityId entityId);

    /// <summary>
    /// Get all the datoms for the given entity id.
    /// </summary>
    public IndexSegment Datoms(EntityId id);

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
    IndexSegment Datoms<TValue, TLowLevel>(Attribute<TValue, TLowLevel> attribute, TValue value);

    /// <summary>
    /// Finds all the datoms that have the given attribute with the given value.
    /// </summary>
    IndexSegment Datoms(ReferenceAttribute attribute, EntityId value);
    
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
}
