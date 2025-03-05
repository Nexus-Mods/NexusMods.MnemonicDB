using System;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.EventTypes;

/// <summary>
/// An event that requests to observe datoms from a specific range.
/// </summary>
public record ObserveDatomsEvent<TDescriptor>(TDescriptor Descriptor, IObserver<ChangeSet<Datom, DatomKey, IDb>> Observer) : IObserveDatomsEvent
where TDescriptor : ISliceDescriptor
{
    public (Datom From, Datom To, IObserver<ChangeSet<Datom, DatomKey, IDb>> Observer) Prime(IDb db)
    {
        var datoms = db.Datoms(Descriptor);
        var cache = db.AttributeCache;
        var changes = new ChangeSet<Datom, DatomKey, IDb>(db);

        foreach (var datom in datoms)
        {
            var isMany = cache.IsCardinalityMany(datom.A);
            changes.Add(new Change<Datom, DatomKey>(ChangeReason.Add, Connection.CreateKey(datom, datom.A, isMany), datom));
        }
        
        Observer.OnNext(changes);
        var (from, to, _) = Descriptor;
        return (from, to, Observer);
    }
}
