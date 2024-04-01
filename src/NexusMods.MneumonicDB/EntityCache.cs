using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;
using NexusMods.MneumonicDB.Abstractions.Models;
using NexusMods.MneumonicDB.Storage;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB;

internal class EntityCache(Db db, AttributeRegistry registry)
{
    private ConcurrentDictionary<EntityId, Entry> cache = new();


    private class InlineCache<TAttribute, TValue> where TAttribute : IAttribute<TValue>
    {
        public AttributeRegistry Registry = default!;
        public TAttribute Attribute = default!;
        public IValueSerializer<TValue> Serializer = default!;
        public ushort Id = 0;

        public static InlineCache<TAttribute, TValue> Instance = new();
    }



    public TValue Get<TAttribute, TValue>(ref ModelHeader header)
    where TAttribute : IAttribute<TValue>
    {
        Entry entry;
        if (header.InlineCache is Entry inline)
            entry = inline;
        else if (!cache.TryGetValue(header.Id, out entry!))
            entry = Cache(header.Id);

        header.InlineCache = entry;

        var localCache = InlineCache<TAttribute, TValue>.Instance;
        if (localCache.Registry != registry)
        {
            localCache = Recache<TAttribute, TValue>();
        }

        var attributeIndex = Array.IndexOf(entry.Attributes, localCache.Id);
        if (attributeIndex == -1)
            throw new KeyNotFoundException();

        var offset = entry.Offsets[attributeIndex];
        var length = entry.Offsets[attributeIndex + 1] - offset;

        localCache.Serializer.Read(entry.Data.AsSpan().SliceFast(offset, length), out var value);
        return value;
    }

    private InlineCache<TAttribute, TValue> Recache<TAttribute, TValue>() where TAttribute : IAttribute<TValue>
    {
        InlineCache<TAttribute, TValue> localCache;
        var attrId = registry.GetAttributeId<TAttribute>();
        var attribute = (IAttribute<TValue>)registry.GetAttribute(attrId);
        localCache = new InlineCache<TAttribute, TValue>()
        {
            Registry = registry,
            Attribute = (TAttribute)attribute,
            Serializer = attribute.Serializer,
            Id = (ushort)attrId.Value
        };
        InlineCache<TAttribute, TValue>.Instance = localCache;
        return localCache;
    }

    private Entry Cache(EntityId id)
    {
        List<ushort> attributes = new();
        List<ushort> offsets = new();

        PooledMemoryBufferWriter writer = new();

        using var iterator = db.Snapshot.GetIterator(IndexType.EAVTCurrent);
        var section = iterator.SeekTo(id).While(id);

        unsafe
        {
            while (section.Valid)
            {
                var keySpan = section.CurrentKeyPrefix();
                attributes.Add((ushort)keySpan.A.Value);
                offsets.Add((ushort)writer.Length);

                writer.Write(section.Current.SliceFast(sizeof(KeyPrefix)));
                section.Next();
            }
        }
        offsets.Add((ushort)writer.Length);

        var entry = new Entry
        {
            Attributes = attributes.ToArray(),
            Offsets = offsets.ToArray(),
            Data = writer.WrittenSpanWritable.ToArray()
        };

        cache[id] = entry;

        return entry;
    }

    private class Entry
    {
        public ushort[] Attributes = null!;
        public ushort[] Offsets = null!;
        public byte[] Data = null!;
    }

    public IEnumerable<TValue> GetAll<TAttribute, TValue>(ref ModelHeader header)
        where TAttribute : IAttribute<TValue>
    {
        Entry entry;
        if (header.InlineCache is Entry inline)
            entry = inline;
        else if (!cache.TryGetValue(header.Id, out entry!))
            entry = Cache(header.Id);

        header.InlineCache = entry;

        return GetAllInner<TAttribute, TValue>(entry);
    }

    private IEnumerable<TValue> GetAllInner<TAttribute, TValue>(Entry entry) where TAttribute : IAttribute<TValue>
    {
        var localCache = InlineCache<TAttribute, TValue>.Instance;
        if (localCache.Registry != registry)
        {
            localCache = Recache<TAttribute, TValue>();
        }

        var attributeIndex = Array.IndexOf(entry.Attributes, localCache.Id);
        if (attributeIndex == -1)
            yield break;

        while (attributeIndex < entry.Attributes.Length)
        {
            if (entry.Attributes[attributeIndex] != localCache.Id)
                break;

            var offset = entry.Offsets[attributeIndex];
            var length = entry.Offsets[attributeIndex + 1] - offset;

            localCache.Serializer.Read(entry.Data.AsSpan().SliceFast(offset, length), out var value);
            yield return value;
            attributeIndex++;
        }
    }
}
