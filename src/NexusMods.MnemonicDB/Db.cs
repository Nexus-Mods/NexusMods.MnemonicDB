using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Caching;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB;

internal class Db : IDb
{
    private readonly AttributeRegistry _registry;
    private readonly IndexSegmentCache _cache;
    private readonly RegistryId _registryId;
    
    /// <summary>
    /// The connection is used by several methods to navigate the graph of objects of Db, Connection, Datom Store, and
    /// Attribute Registry. However, we want the Datom Store and Connection to be decoupled, so the Connection starts null
    /// and is set by the Connection class after the Datom Store has pushed the Db object to it.
    /// </summary>
    private IConnection? _connection;
    public ISnapshot Snapshot { get; }
    public IAttributeRegistry Registry => _registry;

    public IndexSegment RecentlyAdded { get; }
    
    internal Dictionary<Type, object> AnalyzerData { get; } = new();

    public Db(ISnapshot snapshot, TxId txId, AttributeRegistry registry)
    {
        Debug.Assert(snapshot != null, $"{nameof(snapshot)} cannot be null");
        _registryId = registry.Id;
        _registry = registry;
        _cache = new IndexSegmentCache();
        Snapshot = snapshot;
        BasisTxId = txId;
        RecentlyAdded = snapshot.Datoms(SliceDescriptor.Create(txId, registry));
    }

    private Db(ISnapshot snapshot, TxId txId, AttributeRegistry registry, RegistryId registryId, IConnection connection, IndexSegmentCache newCache, IndexSegment recentlyAdded)
    {
        _registry = registry;
        _registryId = registryId;
        _cache = newCache;
        _connection = connection;
        Snapshot = snapshot;
        BasisTxId = txId;
        RecentlyAdded = recentlyAdded;
    }

    /// <summary>
    /// Create a new Db instance with the given store result and transaction id integrated, will evict old items
    /// from the cache, and update the cache with the new datoms.
    /// </summary>
    internal Db WithNext(StoreResult storeResult, TxId txId)
    {
        var newCache = _cache.ForkAndEvict(storeResult, _registry, out var newDatoms);
        return new Db(storeResult.Snapshot, txId, _registry, _registryId, _connection!, newCache, newDatoms);
    }

    private IndexSegment EntityDatoms(IDb db, EntityId id)
    {
        return _cache.Get(id, db);
    }

    private static IndexSegment ReverseDatoms(IDb db, (EntityId, AttributeId) key)
    {
        return db.Snapshot.Datoms(SliceDescriptor.Create(key.Item2, key.Item1, db.Registry));
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
    public IndexSegment Get(EntityId entityId)
    {
        return Datoms(entityId);
    }

    public EntityIds GetBackRefs(ReferenceAttribute attribute, EntityId id)
    {
        var segment = _cache.GetReverse(attribute.GetDbId(_registryId), id, this);
        return new EntityIds(segment, 0, segment.Count);
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

    public IndexSegment Datoms<TValue, TLowLevel>(Attribute<TValue, TLowLevel> attribute, TValue value)
    {
        return Datoms(SliceDescriptor.Create(attribute, value, _registry));
    }

    public IndexSegment Datoms(ReferenceAttribute attribute, EntityId value)
    {
        return Datoms(SliceDescriptor.Create(attribute, value, _registry));
    }

    public IndexSegment Datoms(EntityId entityId)
    {
        return _cache.Get(entityId, this);
    }
    
    public IndexSegment Datoms(IAttribute attribute)
    {
        return Snapshot.Datoms(SliceDescriptor.Create(attribute, _registry));
    }

    public IndexSegment Datoms(SliceDescriptor sliceDescriptor)
    {
        return Snapshot.Datoms(sliceDescriptor);
    }

    public IndexSegment Datoms(TxId txId)
    {
        return Snapshot.Datoms(SliceDescriptor.Create(txId, _registry));
    }

    public bool Equals(IDb? other)
    {
        if (other is null)
            return false;
        return ReferenceEquals(_connection, other.Connection)
               && BasisTxId.Equals(other.BasisTxId);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Db)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_connection, BasisTxId);
    }
}
