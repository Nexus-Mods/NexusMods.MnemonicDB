using System;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.EventTypes;

public interface IObserveDatomsEvent : IEvent
{
    internal (Datom From, Datom To, IObserver<ChangeSet<Datom, DatomKey, IDb>> Observer) Prime(IDb db);
}
