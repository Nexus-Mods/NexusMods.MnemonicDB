using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB;

internal class Db<TSnapshot, TLowLevelIterator> : ACachingDatomsIndex<TLowLevelIterator>, IDb 
    where TSnapshot : IRefDatomEnumeratorFactory<TLowLevelIterator>, IDatomsIndex, ISnapshot
    where TLowLevelIterator : IRefDatomEnumerator
{
    //private readonly IndexSegmentCache _cache;
    
    /// <summary>
    /// The connection is used by several methods to navigate the graph of objects of Db, Connection, Datom Store, and
    /// Attribute Registry. However, we want the Datom Store and Connection to be decoupled, so the Connection starts null
    /// and is set by the Connection class after the Datom Store has pushed the Db object to it.
    /// </summary>
    private IConnection? _connection;
    public ISnapshot Snapshot => _snapshot;

    private readonly Lazy<IndexSegment> _recentlyAdded;
    private readonly TSnapshot _snapshot;
    public IndexSegment RecentlyAdded => _recentlyAdded.Value;

    internal Dictionary<Type, object> AnalyzerData { get; } = new();

    internal Db(TSnapshot snapshot, TxId txId, AttributeCache attributeCache, IConnection? connection = null) : base(attributeCache)
    {
        _connection = connection;
        _snapshot = snapshot;
        BasisTxId = txId;
        _recentlyAdded = new (() => snapshot.Datoms(SliceDescriptor.Create(txId)));
    }

    internal Db(TSnapshot newSnapshot, TxId newTxId, IndexSegment addedDatoms, Db<TSnapshot, TLowLevelIterator> src) : base(src, addedDatoms) 
    {
        _connection = src._connection;
        _snapshot = newSnapshot;
        BasisTxId = newTxId;
        _recentlyAdded = new Lazy<IndexSegment>(() => addedDatoms);
    }

    /// <summary>
    /// Create a new Db instance with the given store result and transaction id integrated, will evict old items
    /// from the cache, and update the cache with the new datoms.
    /// </summary>
    public IDb WithNext(StoreResult storeResult, TxId txId)
    {
        var newDatoms = storeResult.Snapshot.Datoms(txId);
        return new Db<TSnapshot, TLowLevelIterator>((TSnapshot)storeResult.Snapshot, txId, newDatoms, this);
    }
    
    public void Analyze(IDb? prev, IAnalyzer[] analyzers)
    {
        foreach (var analyzer in analyzers)
        {
            var result = analyzer.Analyze(prev, this);
            AnalyzerData[analyzer.GetType()] = result;
        }
    }

    public TxId BasisTxId { get; }

    public IConnection Connection
    {
        get
        {
            Debug.Assert(_connection != null, "Connection is not set");
            return _connection!;
        }
        set
        {
            Debug.Assert(_connection == null || ReferenceEquals(_connection, value), "Connection is already set");
            _connection = value;
        }
    }
    
    public Entities<TModel> GetBackrefModels<TModel>(AttributeId aid, EntityId id) where TModel : IReadOnlyModel<TModel> 
        => GetBackrefModels<TModel>(this, aid, id);

    TReturn IDb.AnalyzerData<TAnalyzer, TReturn>()
    {
        if (AnalyzerData.TryGetValue(typeof(TAnalyzer), out var value))
            return (TReturn)value;
        throw new KeyNotFoundException($"Analyzer {typeof(TAnalyzer).Name} not found");
    }

    public void ClearIndexCache()
    {
        EntityCache.Clear();
        BackReferenceCache.Clear();
    }
    
    public IndexSegment Datoms<TValue>(IWritableAttribute<TValue> attribute, TValue value)
    {
        return Datoms(SliceDescriptor.Create(attribute, value, AttributeCache));
    }
    
    public IndexSegment Datoms(IAttribute attribute)
    {
        return Datoms(SliceDescriptor.Create(attribute, AttributeCache));
    }

    [MustDisposeResource]
    public override TLowLevelIterator GetRefDatomEnumerator(bool totalOrdered) => _snapshot.GetRefDatomEnumerator(totalOrdered);

    public IndexSegment Datoms(TxId txId)
    {
        return Snapshot.Datoms(SliceDescriptor.Create(txId));
    }

    public bool Equals(IDb? other)
    {
        if (other is null)
            return false;
        return ReferenceEquals(_connection, other.Connection)
               && BasisTxId.Equals(other.BasisTxId)
               && Snapshot.GetType() == other.Snapshot.GetType();
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((IDb)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_connection, BasisTxId);
    }
}
