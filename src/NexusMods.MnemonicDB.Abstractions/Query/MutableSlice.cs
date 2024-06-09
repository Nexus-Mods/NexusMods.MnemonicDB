using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions.Query;

public static class MutableSlice
{

    /// <summary>
    /// Observe a slice of the database, as datoms are added or removed from the database, the observer will be updated
    /// with the changeset of datoms that have been added or removed.
    /// </summary>
    public static IObservable<IChangeSet<Datom>> Observe(IConnection conn, SliceDescriptor descriptor)
    {
        var set = new SortedSet<Datom>(PartialComparator(descriptor.Index));

        return conn.Revisions.Select((db, idx) =>
        {
            if (idx == 0)
                return Setup(set, db, descriptor);
            return Diff(set, db, descriptor);
        });
    }

    private static IChangeSet<Datom> Diff(SortedSet<Datom> set, IDb db, SliceDescriptor descriptor)
    {
        var updates = db.Datoms(SliceDescriptor.Create(db.BasisTxId, db.Registry));
        List<Change<Datom>>? changes = null;

        foreach (var datom in updates)
        {
            if (!descriptor.Includes(datom))
                continue;
            if (datom.IsRetract)
            {
                var idx = set.IndexOf(datom);
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

    private struct Comparer<TInner> : IComparer<Datom>
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
