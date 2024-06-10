using System;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// A dynamic cache of entities in the connection filtered by various criteria.
/// </summary>
public interface IDynamicCache
{
    /// <summary>
    /// Get all the entities of a given type that are in the cache. When an entity becomes
    /// invalid it will be removed from the cache.
    /// </summary>
    public IObservable<IChangeSet<Attribute<THighLevel, TLowLevel>.ReadDatom, EntityId>>
        Query<TModel, THighLevel, TLowLevel>(Attribute<THighLevel, TLowLevel> attr, THighLevel value);
}
