﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions.Query;

public static class ObservableDatoms
{

    /// <summary>
    /// Observe a slice of the database, as datoms are added or removed from the database, the observer will be updated
    /// with the changeset of datoms that have been added or removed.
    /// </summary>
    public static IObservable<IChangeSet<Datom>> ObserveDatoms(this IConnection conn, SliceDescriptor descriptor)
    {
        var comparator = PartialComparator(descriptor.Index);
        var equality = (IEqualityComparer<Datom>)comparator;
        var set = new SortedSet<Datom>(comparator);

        return conn.Revisions.Select((rev, idx) =>
        {
            if (idx == 0)
                return Setup(set, rev.Database, descriptor);
            return Diff(set, rev.AddedDatoms, descriptor, equality);
        });
    }

    /// <summary>
    /// Observe all datoms for a given entity id
    /// </summary>
    public static IObservable<IChangeSet<Datom>> ObserveDatoms(this IConnection conn, EntityId id)
    {
        return conn.ObserveDatoms(SliceDescriptor.Create(id, conn.Registry));
    }

    private static IChangeSet<Datom> Diff(SortedSet<Datom> set, IndexSegment updates, SliceDescriptor descriptor, IEqualityComparer<Datom> comparer)
    {
        List<Change<Datom>>? changes = null;

        foreach (var datom in updates)
        {
            if (!descriptor.Includes(datom))
                continue;
            if (datom.IsRetract)
            {
                var idx = set.IndexOf(datom, comparer);
                if (idx >= 0)
                {
                    set.Remove(datom);
                    changes ??= [];
                    changes.Add(new Change<Datom>(ListChangeReason.Remove, datom, idx));
                }
                else
                {
                    throw new InvalidOperationException("Retract without assert in set");
                }
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
        public unsafe int Compare(Datom a, Datom b)
        {
            var aSpan = a.RawSpan;
            var bSpan = b.RawSpan;
            fixed(byte* aPtr = aSpan)
            fixed(byte* bPtr = bSpan)
                return TInner.Compare(aPtr, aSpan.Length, bPtr, bSpan.Length);
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
