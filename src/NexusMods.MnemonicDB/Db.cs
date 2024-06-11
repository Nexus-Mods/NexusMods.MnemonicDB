using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB;

internal class Db : IDb
{
    private readonly Connection _connection;
    private readonly AttributeRegistry _registry;

    private readonly IndexSegmentCache<EntityId> _entityCache;
    private readonly IndexSegmentCache<(EntityId, AttributeId)> _reverseCache;
    private readonly IndexSegmentCache<EntityId> _referencesCache;
    private readonly RegistryId _registryId;
    private readonly Lazy<IAnalytics> _analytics;

    public ISnapshot Snapshot { get; }
    public IAttributeRegistry Registry => _registry;

    public Db(ISnapshot snapshot, Connection connection, TxId txId, AttributeRegistry registry)
    {
        Debug.Assert(snapshot != null, $"{nameof(snapshot)} cannot be null");
        _registryId = registry.Id;
        _registry = registry;
        _connection = connection;
        _entityCache = new IndexSegmentCache<EntityId>(EntityDatoms, registry);
        _reverseCache = new IndexSegmentCache<(EntityId, AttributeId)>(ReverseDatoms, registry);
        _referencesCache = new IndexSegmentCache<EntityId>(ReferenceDatoms, registry);
        _analytics = new Lazy<IAnalytics>(() => new Analytics(this));
        Snapshot = snapshot;
        BasisTxId = txId;
    }

    private static IEnumerable<Datom> EntityDatoms(IDb db, EntityId id)
    {
        return db.Snapshot.Datoms(SliceDescriptor.Create(id, db.Registry));
    }

    private static IEnumerable<Datom> ReverseDatoms(IDb db, (EntityId, AttributeId) key)
    {
        return db.Snapshot.Datoms(SliceDescriptor.Create(key.Item2, key.Item1, db.Registry));
    }

    public TxId BasisTxId { get; }

    public IAnalytics Analytics => _analytics.Value;

    public IConnection Connection => _connection;

    public IEnumerable<TModel> Get<TModel>(IEnumerable<EntityId> ids)
        where TModel : IHasEntityIdAndDb
    {
        foreach (var id in ids)
        {
            yield return Get<TModel>(id);
        }
    }

    /// <summary>
    /// Gets the IndexSegment for the given entity id.
    /// </summary>
    public IndexSegment Get(EntityId entityId)
    {
        return _entityCache.Get(this, entityId);
    }

    public IEnumerable<EntityId> Find(IAttribute attribute)
    {
        var attrId = attribute.GetDbId(_registry.Id);
        return Snapshot
            .Datoms(SliceDescriptor.Create(attrId, _registry))
            .Select(d => d.E);
    }

    public EntityIds GetBackRefs(ReferenceAttribute attribute, EntityId id)
    {
        var segment = _reverseCache.Get(this, (id, attribute.GetDbId(_registry.Id)));
        return new EntityIds(segment, 0, segment.Count);
    }

    private static IEnumerable<Datom> ReferenceDatoms(IDb db, EntityId eid)
    {
        return db.Snapshot.Datoms(SliceDescriptor.CreateReferenceTo(eid, db.Registry));
    }

    public IndexSegment ReferencesTo(EntityId id)
    {
        return _referencesCache.Get(this, id);
    }

    public IndexSegment GetSegment(EntityId id)
    {
        var a = KeyPrefix.Min;
        var b = new KeyPrefix().Set(EntityId.MaxValueNoPartition, AttributeId.Max, TxId.MaxValue, false);
        return _entityCache.Get(this, id);
    }

    public IEnumerable<TValue> GetAll<TValue, TLowLevel>(EntityId id, Attribute<TValue, TLowLevel> attribute)
    {
        var attrId = attribute.GetDbId(_registry.Id);
        var results = _entityCache.Get(this, id)
            .Where(d => d.A == attrId)
            .Select(d => d.Resolve(attribute));

        return results;
    }

    public IEnumerable<EntityId> FindIndexed<TValue, TLowLevel>(Attribute<TValue, TLowLevel> attribute, TValue value)
    {
        return FindIndexedDatoms(attribute, value)
            .Select(d => d.E);
    }

    public IEnumerable<Datom> FindIndexedDatoms<TValue, TLowLevel>(Attribute<TValue, TLowLevel> attribute, TValue value)
    {
        if (!attribute.IsIndexed)
            throw new InvalidOperationException($"Attribute {attribute.Id} is not indexed");

        return Snapshot
            .Datoms(SliceDescriptor.Create(attribute, value, _registry));;
    }

    public TModel Get<TModel>(EntityId id)
        where TModel : IHasEntityIdAndDb
    {
        return EntityConstructors<TModel>.Constructor(id, this);
    }

    public Entities<EntityIds, TModel> GetReverse<TModel>(EntityId id, Attribute<EntityId, ulong> attribute)
        where TModel : IReadOnlyModel<TModel>
    {
        var segment = _reverseCache.Get(this, (id, attribute.GetDbId(_registry.Id)));
        var ids = new EntityIds(segment, 0, segment.Count);
        return new Entities<EntityIds, TModel>(ids, this);
    }

    public IEnumerable<IReadDatom> Datoms(EntityId entityId)
    {
        return _entityCache.Get(this, entityId)
            .Select(d => d.Resolved);
    }

    public IndexSegment Datoms(SliceDescriptor sliceDescriptor)
    {
        return Snapshot.Datoms(sliceDescriptor);
    }

    public IEnumerable<IReadDatom> Datoms(TxId txId)
    {
        return Snapshot.Datoms(SliceDescriptor.Create(txId, _registry))
            .Select(d => d.Resolved);
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
