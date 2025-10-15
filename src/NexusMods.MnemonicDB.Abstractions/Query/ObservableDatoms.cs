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
using NexusMods.MnemonicDB.Abstractions.Traits;

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
}
