using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Query;

public static class ObservableDatoms
{

    /// <summary>
    /// Delays the creation of the IObservable until after the first value from the src is received. Once the first
    /// value arrives the ctor function will be called to create the observable. This is useful for situations where
    /// the creation of an observable is not valid until some startup operations have been performed that will eventually
    /// publish a value to an observable.
    /// </summary>
    public static IObservable<TResult> DelayUntilFirstValue<TSrc, TResult>(this IObservable<TSrc> src,
        Func<IObservable<TResult>> constructorFn)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(constructorFn);

        return src
            .Take(1)
            .Select(_ => constructorFn())
            .Switch();
    }

    /// <summary>
    /// Observe a slice of the database, as datoms are added or removed from the database, the observer will be updated
    /// with the changeset of datoms that have been added or removed.
    /// </summary>
    public static IObservable<IChangeSet<Datom, DatomKey>> ObserveDatoms(this IConnection conn, SliceDescriptor descriptor)
    {
        var lastTxId = TxId.From(0);

        return conn.Revisions
            .Where(rev => rev.RecentlyAdded.Count > 0)
            .Select<IDb, IChangeSet<Datom, DatomKey>>((rev, idx) =>
        {
            if (rev.BasisTxId <= lastTxId && idx != 0)
                return ChangeSet<Datom, DatomKey>.Empty;

            lastTxId = rev.BasisTxId;

            if (idx == 0)
                return Setup(rev, descriptor);
            return Diff(conn.Registry, rev.RecentlyAdded, descriptor);
        });
    }

    /// <summary>
    /// Observe all datoms for a given entity id
    /// </summary>
    public static IObservable<IChangeSet<Datom, DatomKey>> ObserveDatoms(this IConnection conn, EntityId id)
    {
        return conn.ObserveDatoms(SliceDescriptor.Create(id, conn.Registry));
    }

    /// <summary>
    /// Observe changes for a given attribute on a given entity id
    /// </summary>
    public static IObservable<IChangeSet<Datom, DatomKey>> ObserveDatoms(this IConnection conn, EntityId id, IAttribute attribute)
    {

        return conn.Revisions.DelayUntilFirstValue(() => conn.ObserveDatoms(SliceDescriptor.Create(id, attribute.GetDbId(conn.Registry.Id), conn.Registry)));
    }

    /// <summary>
    /// Observe changes for datoms that point to the given entity id via the given attribute
    /// </summary>
    public static IObservable<IChangeSet<Datom, DatomKey>> ObserveDatoms(this IConnection conn, ReferenceAttribute attribute, EntityId id)
    {
        return conn.Revisions.DelayUntilFirstValue(() => conn.ObserveDatoms(SliceDescriptor.Create(attribute, id, conn.Registry)));
    }

    /// <summary>
    /// Observe changes for a given attribute
    /// </summary>
    public static IObservable<IChangeSet<Datom, DatomKey>> ObserveDatoms(this IConnection conn, IAttribute attribute)
    {
        return conn.Revisions.DelayUntilFirstValue(() => conn.ObserveDatoms(SliceDescriptor.Create(attribute, conn.Registry)));
    }
    
    /// <summary>
    /// Converts a set of observed datoms to a set of observed entity ids, assumes that there will be no datoms with
    /// duplicate entity ids.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static IObservable<IChangeSet<Datom, EntityId>> AsEntityIds(this IObservable<IChangeSet<Datom, DatomKey>> source)
    {
        return source.Select(changes =>
        {
            var newChanges = new ChangeSet<Datom, EntityId>();
            foreach (var change in changes)
            {
                newChanges.Add(new Change<Datom, EntityId>(change.Reason, change.Key.E, change.Current));
            }
            return newChanges;
        });
    }

    private static IChangeSet<Datom, DatomKey> Diff(IAttributeRegistry registry, IndexSegment updates, SliceDescriptor descriptor)
    {
        var changes = new ChangeSet<Datom, DatomKey>();
        
        AttributeId previousAid = default;
        IAttribute attr = default!;
        bool isMany = false;
        
        for (int i = 0; i < updates.Count; i++) 
        {
            var datom = updates[i];
            if (!descriptor.Includes(datom))
                continue;
            UpdateAttrCache(registry, datom.A, ref previousAid, ref attr, ref isMany);
            if (datom.IsRetract)
            {


                // If the attribute is cardinality many, we can just remove the datom
                if (isMany)
                {
                    changes.Add(new Change<Datom, DatomKey>(ChangeReason.Remove, CreateKey(datom, attr, true), datom));
                    continue;
                }

                // If the next datom is not the same E or A, we can remove the datom
                if (updates.Count <= i + 1)
                {
                    changes.Add(new Change<Datom, DatomKey>(ChangeReason.Remove, CreateKey(datom, attr), datom));
                    continue;
                }

                var nextDatom = updates[i + 1];
                if (nextDatom.E != datom.E || nextDatom.A != datom.A)
                {
                    changes.Add(new Change<Datom, DatomKey>(ChangeReason.Remove, CreateKey(datom, attr), datom));
                    continue;
                }

                // Otherwise we skip the add, and issue an update, and skip the add because we've already processed it
                changes.Add(new Change<Datom, DatomKey>(ChangeReason.Update, CreateKey(datom, attr), nextDatom, datom));
                i++;
            }
            else
            {
                changes.Add(new Change<Datom, DatomKey>(ChangeReason.Add, CreateKey(datom, attr, isMany), datom));
            }
        }
        
        if (changes.Count == 0)
            return ChangeSet<Datom, DatomKey>.Empty;
        return changes;
    }

    private static ChangeSet<Datom, DatomKey> Setup(IDb db, SliceDescriptor descriptor)
    {
        var datoms = db.Datoms(descriptor);
        var registry = db.Registry;
        var changes = new ChangeSet<Datom, DatomKey>();
        
        AttributeId previousAid = default;
        IAttribute attr = default!;
        bool isMany = false;
        
        foreach (var datom in datoms)
        {
            UpdateAttrCache(registry, datom.A, ref previousAid, ref attr, ref isMany);
            changes.Add(new Change<Datom, DatomKey>(ChangeReason.Add, CreateKey(datom, attr, isMany), datom));
        }

        if (changes.Count == 0)
            return ChangeSet<Datom, DatomKey>.Empty;
        return changes;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DatomKey CreateKey(Datom datom, IAttribute attribute, bool isMany = false)
    {
        return new DatomKey(datom.E, attribute, isMany ? datom.ValueMemory : Memory<byte>.Empty);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdateAttrCache(IAttributeRegistry registry, AttributeId aid, ref AttributeId cachedAid, ref IAttribute attr, ref bool isMany)
    {
        if (cachedAid == aid)
            return;
        cachedAid = aid;
        attr = registry.GetAttribute(aid);
        isMany = attr.Cardinalty == Cardinality.Many;
    }
}
