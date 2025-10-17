using System;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB.EventTypes;

public interface IObserveDatomsEvent : IEvent
{
    internal (Datom From, Datom To, IObserver<ChangeSet<ValueDatom, DatomKey, IDb>> Observer) Prime(IDb db);
}
