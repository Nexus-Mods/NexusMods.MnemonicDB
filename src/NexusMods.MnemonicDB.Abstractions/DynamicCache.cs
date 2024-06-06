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
    private struct CacheEntry
    {
        public IndexType IndexType;
        public Memory<byte> From;
        public Memory<byte> To;
        public SourceCache<IReadDatom, > Cache;
    }

    private readonly IConnection _connection;
    private readonly KeyBuilder _keyBuilder;
    private readonly ConcurrentBag<(Memory<byte> From, Memo)>

    public DynamicCache(IConnection connection, RegistryId registryId)
    {
        _connection = connection;
        _keyBuilder = new KeyBuilder(registryId);
    }
    public IObservable<IChangeSet<Attribute<THighLevel, TLowLevel>.ReadDatom, EntityId>> Query<TModel, THighLevel, TLowLevel>(Attribute<THighLevel, TLowLevel> attr, THighLevel value)
    {
        var from = _keyBuilder.From(attr, value);
        var to = _keyBuilder.To(attr, value);
    }
}
