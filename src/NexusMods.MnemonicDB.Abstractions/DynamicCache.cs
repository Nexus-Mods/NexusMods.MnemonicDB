using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

public class DynamicCache : IDynamicCache
{
    public IObservable<IChangeSet<Attribute<THighLevel, TLowLevel>.ReadDatom, EntityId>> Query<TModel, THighLevel, TLowLevel>(Attribute<THighLevel, TLowLevel> attr, THighLevel value)
    {
        throw new NotImplementedException();
    }
}
