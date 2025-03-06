using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Caching;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;

namespace NexusMods.MnemonicDB;

internal class Db<TSnapshot, TLowLevelIterator> : ADatomsIndex<TLowLevelIterator>, IDb 
    where TSnapshot : IRefDatomEnumeratorFactory<TLowLevelIterator>, IDatomsIndex, ISnapshot
    where TLowLevelIterator : IRefDatomEnumerator
{
    private readonly IndexSegmentCache _cache;
    
    /// <summary>
    /// The connection is used by several methods to navigate the graph of objects of Db, Connection, Datom Store, and
    /// Attribute Registry. However, we want the Datom Store and Connection to be decoupled, so the Connection starts null
    /// and is set by the Connection class after the Datom Store has pushed the Db object to it.
    /// </summary>
    private IConnection? _connection;
    public ISnapshot Snapshot => _snapshot;
    public AttributeCache AttributeCache { get; }

    private readonly Lazy<IndexSegment> _recentlyAdded;
    private readonly TSnapshot _snapshot;
    public IndexSegment RecentlyAdded => _recentlyAdded.Value;

    internal Dictionary<Type, object> AnalyzerData { get; } = new();

    internal Db(TSnapshot snapshot, TxId txId, AttributeCache attributeCache, IConnection? connection = null, object? newCache = null, IndexSegment? recentlyAdded = null) : base(attributeCache)
    {
        AttributeCache = attributeCache;
        
        if (newCache is null)
            _cache = new IndexSegmentCache();
        else
            _cache = (IndexSegmentCache)newCache;
        _connection = connection;
        _snapshot = snapshot;
        BasisTxId = txId;
        
        if (recentlyAdded is null)
            _recentlyAdded = new (() => snapshot.Datoms(SliceDescriptor.Create(txId)));
        else
            _recentlyAdded = new (() => recentlyAdded.Value);
    }

    /// <summary>
    /// Create a new Db instance with the given store result and transaction id integrated, will evict old items
    /// from the cache, and update the cache with the new datoms.
    /// </summary>
    public IDb WithNext(StoreResult storeResult, TxId txId)
    {
        var newCache = _cache.ForkAndEvict(storeResult, AttributeCache, out var newDatoms);
        return storeResult.Snapshot.MakeDb(txId, AttributeCache, _connection!, newCache, newDatoms);
    }

    public void AddAnalyzerData(Type getType, object result)
    {
        AnalyzerData.Add(getType, result);
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

    /// <summary>
    /// Gets the IndexSegment for the given entity id.
    /// </summary>
    public EntitySegment Get(EntityId entityId)
    {
        return Datoms(entityId);
    }

    public EntityIds GetBackRefs(ReferenceAttribute attribute, EntityId id)
    {
        var aid = _connection!.AttributeCache.GetAttributeId(attribute.Id);
        var segment = _cache.GetReverse(aid, id, this);
        return segment;
    }
    
    public IndexSegment ReferencesTo(EntityId id)
    {
        return _cache.GetReferences(id, this);
    }

    TReturn IDb.AnalyzerData<TAnalyzer, TReturn>()
    {
        if (AnalyzerData.TryGetValue(typeof(TAnalyzer), out var value))
            return (TReturn)value;
        throw new KeyNotFoundException($"Analyzer {typeof(TAnalyzer).Name} not found");
    }

    public void ClearIndexCache()
    {
        _cache.Clear();
    }
    
    public IndexSegment Datoms<TValue>(IWritableAttribute<TValue> attribute, TValue value)
    {
        return Datoms(SliceDescriptor.Create(attribute, value, AttributeCache));
    }
    
    public EntitySegment Datoms(EntityId entityId)
    {
        return _cache.Get(entityId, this);
    }
    
    public IndexSegment Datoms(IAttribute attribute)
    {
        return Datoms(SliceDescriptor.Create(attribute, AttributeCache));
    }

    [MustDisposeResource]
    public override TLowLevelIterator GetRefDatomEnumerator() => _snapshot.GetRefDatomEnumerator();

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
