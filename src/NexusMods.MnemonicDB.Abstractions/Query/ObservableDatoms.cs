﻿using System;
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

/// <summary>
/// Extensions for observing datoms in the database
/// </summary>
public static class ObservableDatoms
{
    /// <summary>
    /// Observe all datoms for a given entity id
    /// </summary>
    public static IObservable<IChangeSet<Datom, DatomKey>> ObserveDatoms(this IConnection conn, EntityId id)
    {
        return conn.ObserveDatoms(SliceDescriptor.Create(id));
    }
    
    /// <summary>
    /// Observe changes for datoms that point to the given entity id via the given attribute
    /// </summary>
    public static IObservable<IChangeSet<Datom, DatomKey>> ObserveDatoms(this IConnection conn, ReferenceAttribute attribute, EntityId id)
    {
        return conn.ObserveDatoms(SliceDescriptor.Create(attribute, id, conn.AttributeCache));
    }

    /// <summary>
    /// Observe changes for a given attribute
    /// </summary>
    public static IObservable<IChangeSet<Datom, DatomKey>> ObserveDatoms(this IConnection conn, IAttribute attribute)
    {
        return conn.ObserveDatoms(SliceDescriptor.Create(attribute, conn.AttributeCache));
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
                newChanges.Add(new Change<Datom, EntityId>(change.Reason, change.Key.E, change.Current, change.Previous, change.CurrentIndex, change.PreviousIndex));
            }
            return newChanges;
        });
    }

    private static IChangeSet<Datom, DatomKey> Diff(AttributeCache cache, IndexSegment updates, SliceDescriptor descriptor)
    {
        var changes = new ChangeSet<Datom, DatomKey>();
        var index = descriptor.Index;
        
        for (var i = 0; i < updates.Count; i++) 
        {
            var datom = updates[i].WithIndex(index);
            if (!descriptor.Includes(datom))
                continue;
            var isMany = cache.IsCardinalityMany(datom.A);
            var attr = datom.A;
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

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DatomKey CreateKey(Datom datom, AttributeId attrId, bool isMany = false)
    {
        return new DatomKey(datom.E, attrId, isMany ? datom.ValueMemory : Memory<byte>.Empty);
    }
}
