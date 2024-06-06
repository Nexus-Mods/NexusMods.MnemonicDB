using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

public class DynamicCache : IDynamicCache
{
    private readonly ConcurrentDictionary<Type,object> _allEntities;
    private readonly IConnection _connection;

    public DynamicCache(IConnection connection)
    {
        _connection = connection;
        _allEntities = new ConcurrentDictionary<Type, object>();
    }

    public IObservable<IChangeSet<TModel, EntityId>> Entities<TModel>(IAttribute[] watchAttributes)
        where TModel : IRepository<TModel>
    {
        throw new NotImplementedException();
    }

    public IObservable<IChangeSet<TModel, EntityId>> Entities<TModel>() where TModel : IRepository<TModel>
    {
        throw new NotImplementedException();
    }

    public IObservable<IChangeSet<TModel, EntityId>> Entities<TModel, THighLevel, TLowLevel>(Attribute<THighLevel, TLowLevel> attr, THighLevel value)
        where TModel : IRepository<TModel>
    {

        throw new NotImplementedException();

    }
}
