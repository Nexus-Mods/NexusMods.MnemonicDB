using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Caching;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;

namespace NexusMods.MnemonicDB;

internal class Db : IDb
{
    private readonly IndexSegmentCache _cache;
    
    /// <summary>
    /// The connection is used by several methods to navigate the graph of objects of Db, Connection, Datom Store, and
    /// Attribute Registry. However, we want the Datom Store and Connection to be decoupled, so the Connection starts null
    /// and is set by the Connection class after the Datom Store has pushed the Db object to it.
    /// </summary>
    private IConnection? _connection;
    public ISnapshot Snapshot { get; }
    public AttributeCache AttributeCache { get; }

    private readonly Lazy<IndexSegment> _recentlyAdded;
    public IndexSegment RecentlyAdded => _recentlyAdded.Value;

    internal Dictionary<Type, object> AnalyzerData { get; } = new();

    public Db(ISnapshot snapshot, TxId txId, AttributeCache attributeCache)
    {
        Debug.Assert(snapshot != null, $"{nameof(snapshot)} cannot be null");
        AttributeCache = attributeCache;
        _cache = new IndexSegmentCache();
        Snapshot = snapshot;
        BasisTxId = txId;
        
        // We may never need this data, so load it lazily
        _recentlyAdded = new (() => snapshot.Datoms(SliceDescriptor.Create(txId)));
    }

    private Db(ISnapshot snapshot, TxId txId, AttributeCache attributeCache, IConnection connection, IndexSegmentCache newCache, IndexSegment recentlyAdded)
    {
        AttributeCache = attributeCache;
        _cache = newCache;
        _connection = connection;
        Snapshot = snapshot;
        BasisTxId = txId;
        _recentlyAdded = new Lazy<IndexSegment>(recentlyAdded);
    }

    /// <summary>
    /// Create a new Db instance with the given store result and transaction id integrated, will evict old items
    /// from the cache, and update the cache with the new datoms.
    /// </summary>
    internal Db WithNext(StoreResult storeResult, TxId txId)
    {
        var newCache = _cache.ForkAndEvict(storeResult, AttributeCache, out var newDatoms);
        return new Db(storeResult.Snapshot, txId, AttributeCache, _connection!, newCache, newDatoms);
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
        var aid = _connection!.AttributeCache.GetAttributeId(attribute.Id);
        var segment = _cache.GetReverse(aid, id, this);
        return segment.EntityIds();
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

    public Task PrecacheAll()
    {
        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                var casted = (Snapshot)Snapshot;
                using var builder = new IndexSegmentBuilder(AttributeCache);
                using var enumerator =
                    casted.RefDatoms(SliceDescriptor.AllEntities(PartitionId.Entity)).GetEnumerator();
                EntityId currentEntity = default;
                while (enumerator.MoveNext())
                {
                    if (enumerator.KeyPrefix.E != currentEntity && builder.Count > 0)
                    {
                        var built = builder.Build();
                        builder.Reset();
                        _cache.Add(currentEntity, built);
                    }

                    currentEntity = enumerator.KeyPrefix.E;
                    builder.AddCurrent(enumerator);
                }

                if (builder.Count > 0)
                {
                    var built = builder.Build();
                    builder.Reset();
                    _cache.Add(currentEntity, built);
                }


                using var reverseIndexes = casted
                    .RefDatoms(SliceDescriptor.AllReverseAttributesInPartition(PartitionId.Entity)).GetEnumerator();
                var previousAid = default(AttributeId);
                var previousEntity = default(EntityId);

                while (reverseIndexes.MoveNext())
                {
                    if ((reverseIndexes.KeyPrefix.A != previousAid || reverseIndexes.KeyPrefix.E != previousEntity) &&
                        builder.Count > 0)
                    {
                        var built = builder.Build();
                        builder.Reset();
                        _cache.AddReverse(previousEntity, previousAid, built);
                    }

                    previousAid = reverseIndexes.KeyPrefix.A;
                    previousEntity = reverseIndexes.KeyPrefix.E;
                    builder.AddCurrent(reverseIndexes);
                }
                tcs.SetResult();
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return;
            }
        })
        {
            Name = "MnemonicDB Precache",
            IsBackground = true
        };
        thread.Start();
        return tcs.Task;
    }

    public IndexSegment Datoms<TValue>(IWritableAttribute<TValue> attribute, TValue value)
    {
        return Datoms(SliceDescriptor.Create(attribute, value, AttributeCache));
    }
    
    public IndexSegment Datoms(EntityId entityId)
    {
        return _cache.Get(entityId, this);
    }

    public IndexSegment Datoms<TDescriptor>(TDescriptor descriptor) where TDescriptor : ISliceDescriptor
    {
        return Snapshot.Datoms(descriptor);
    }

    public IndexSegment Datoms(IAttribute attribute)
    {
        return Snapshot.Datoms(SliceDescriptor.Create(attribute, AttributeCache));
    }

    public IndexSegment Datoms(SliceDescriptor sliceDescriptor)
    {
        return Snapshot.Datoms(sliceDescriptor);
    }

    public IndexSegment Datoms(TxId txId)
    {
        return Snapshot.Datoms(SliceDescriptor.Create(txId));
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
