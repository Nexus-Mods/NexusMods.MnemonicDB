using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
    public static IObservable<IChangeSet<Datom>> ObserveDatoms(this IConnection conn, SliceDescriptor descriptor)
    {
        var comparator = PartialComparator(descriptor.Index);
        var equality = (IEqualityComparer<Datom>)comparator;
        var set = new SortedSet<Datom>(comparator);
        var lastTxId = TxId.From(0);

        return conn.Revisions
            .Where(rev => rev.RecentlyAdded.Count > 0)
            .Select((rev, idx) =>
        {
            lock (set)
            {
                if (rev.BasisTxId <= lastTxId)
                    return ChangeSet<Datom>.Empty;

                lastTxId = rev.BasisTxId;

                if (idx == 0)
                    return Setup(set, rev, descriptor);
                return Diff(conn.Registry, set, rev.RecentlyAdded, descriptor, equality);
            }
        });
    }

    /// <summary>
    /// Observe all datoms for a given entity id
    /// </summary>
    public static IObservable<IChangeSet<Datom>> ObserveDatoms(this IConnection conn, EntityId id)
    {
        return conn.ObserveDatoms(SliceDescriptor.Create(id, conn.Registry));
    }

    /// <summary>
    /// Observe changes for a given attribute on a given entity id
    /// </summary>
    public static IObservable<IChangeSet<Datom>> ObserveDatoms(this IConnection conn, EntityId id, IAttribute attribute)
    {

        return conn.Revisions.DelayUntilFirstValue(() => conn.ObserveDatoms(SliceDescriptor.Create(id, attribute.GetDbId(conn.Registry.Id), conn.Registry)));
    }

    /// <summary>
    /// Observe changes for datoms that point to the given entity id via the given attribute
    /// </summary>
    public static IObservable<IChangeSet<Datom>> ObserveDatoms(this IConnection conn, ReferenceAttribute attribute, EntityId id)
    {
        return conn.Revisions.DelayUntilFirstValue(() => conn.ObserveDatoms(SliceDescriptor.Create(attribute, id, conn.Registry)));
    }

    /// <summary>
    /// Observe changes for a given attribute
    /// </summary>
    public static IObservable<IChangeSet<Datom>> ObserveDatoms(this IConnection conn, IAttribute attribute)
    {
        return conn.Revisions.DelayUntilFirstValue(() => conn.ObserveDatoms(SliceDescriptor.Create(attribute, conn.Registry)));
    }

    private static IChangeSet<Datom> Diff(IAttributeRegistry registry, SortedSet<Datom> set, IndexSegment updates, SliceDescriptor descriptor, IEqualityComparer<Datom> comparer)
    {
        List<Change<Datom>>? changes = null;
        
        for (int i = 0; i < updates.Count; i++) 
        {
            var datom = updates[i];
            if (!descriptor.Includes(datom))
                continue;
            if (datom.IsRetract)
            {
                var idx = set.IndexOf(datom, comparer);
                if (idx < 0)
                {
                    throw new InvalidOperationException("Retract without assert in set");
                }

                set.Remove(datom);
                changes ??= [];

                // If the attribute is cardinality many, we can just remove the datom
                if (registry.GetAttribute(datom.A).Cardinalty == Cardinality.Many)
                {
                    changes.Add(new Change<Datom>(ListChangeReason.Remove, datom, idx));
                    continue;
                }

                // If the next datom is not the same E or A, we can remove the datom
                if (updates.Count <= i + 1)
                {
                    changes.Add(new Change<Datom>(ListChangeReason.Remove, datom, idx));
                    continue;
                }

                var nextDatom = updates[i + 1];
                if (nextDatom.E != datom.E || nextDatom.A != datom.A)
                {
                    changes.Add(new Change<Datom>(ListChangeReason.Remove, datom, idx));
                    continue;
                }

                // Otherwise we skip the add, and issue a refresh, update the cache, and skip the next datom
                // because we've already processed it
                changes.Add(new Change<Datom>(ListChangeReason.Refresh, nextDatom, datom, idx));
                set.Add(nextDatom);
                i++;
            }
            else
            {
                set.Add(datom);
                var idx = set.IndexOf(datom);
                changes ??= [];
                changes.Add(new Change<Datom>(ListChangeReason.Add, datom, idx));
            }
        }
        if (changes == null)
            return ChangeSet<Datom>.Empty;
        return new ChangeSet<Datom>(changes);
    }

    private static ChangeSet<Datom> Setup(SortedSet<Datom> set, IDb db, SliceDescriptor descriptor)
    {
        var datoms = db.Datoms(descriptor);
        set.UnionWith(datoms);
        return new ChangeSet<Datom>([new Change<Datom>(ListChangeReason.AddRange, datoms)]);
    }

    private struct Comparer<TInner> : IComparer<Datom>, IEqualityComparer<Datom>
    where TInner : IDatomComparator
    {
        public int Compare(Datom a, Datom b)
        {
            return TInner.Compare(a, b);
        }

        public bool Equals(Datom x, Datom y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(Datom obj)
        {
            throw new NotSupportedException();
        }
    }

    private static IComparer<Datom> PartialComparator(IndexType type) => type switch
    {
        IndexType.EAVTCurrent => new Comparer<EAVComparator>(),
        IndexType.EAVTHistory => new Comparer<EAVComparator>(),
        IndexType.AVETCurrent => new Comparer<AVEComparator>(),
        IndexType.AVETHistory => new Comparer<AVEComparator>(),
        IndexType.AEVTCurrent => new Comparer<AEVComparator>(),
        IndexType.AEVTHistory => new Comparer<AEVComparator>(),
        IndexType.VAETCurrent => new Comparer<VAEComparator>(),
        IndexType.VAETHistory => new Comparer<VAEComparator>(),
        _ => throw new ArgumentOutOfRangeException()
    };
}
