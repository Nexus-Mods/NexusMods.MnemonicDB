using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions;
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

        var attrs = entry.Attributes;
        var attributeIndex = attrs.IndexOf(localCache.Id);
        if (attributeIndex == -1)
            throw new KeyNotFoundException();

        var offsets = entry.Offsets;
        var offset = offsets[attributeIndex];
        var length = offsets[attributeIndex + 1] - offset;

        localCache.Serializer.Read(entry.Data.SliceFast((int)offset, (int)length), out var value);
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
        using var iterator = db.Snapshot.GetIterator(IndexType.EAVTCurrent);
        var section = iterator.SeekTo(id).While(id);

        var builder = new CacheBuilder();

        while (section.Valid)
        {
            builder.Add(section.Current);
            section.Next();
        }
        var entry = builder.Build();
        cache[id] = entry;

        return entry;
    }

    private struct CacheBuilder()
    {
        private readonly List<ushort> _attrs = new();
        private readonly List<uint> _offsets = new();
        private readonly PooledMemoryBufferWriter _writer = new();

        public unsafe void Add(ReadOnlySpan<byte> datom)
        {
            var prefix = KeyPrefix.Read(datom);
            if (prefix.IsRetract)
            {
                _writer.Rewind(datom.Length -sizeof(KeyPrefix));
                _attrs.RemoveAt(_attrs.Count - 1);
                _offsets.RemoveAt(_offsets.Count - 1);
            }
            else
            {
                _attrs.Add((ushort)prefix.A.Value);
                _offsets.Add((uint)_writer.Length);
                _writer.Write(datom.SliceFast(sizeof(KeyPrefix)));
            }
        }

        public Entry Build()
        {
            _offsets.Add((uint)_writer.Length);
            return Entry.Create(CollectionsMarshal.AsSpan(_attrs),
                CollectionsMarshal.AsSpan(_offsets), _writer.GetWrittenSpan());
        }
    }

    private class Entry(Memory<byte> data, int count)
    {

        public ReadOnlySpan<ushort> Attributes => MemoryMarshal.Cast<byte, ushort>(data.Span.SliceFast(0, count * sizeof(ushort)));

        public ReadOnlySpan<uint> Offsets => MemoryMarshal.Cast<byte, uint>(data.Span.SliceFast(count * sizeof(ushort), (count + 1) * sizeof(uint)));

        public ReadOnlySpan<byte> Data => data.Span.SliceFast(count * sizeof(ushort) + (count + 1) * sizeof(uint));

        public static Entry Create(ReadOnlySpan<ushort> attributes, ReadOnlySpan<uint> offsets, ReadOnlySpan<byte> data)
        {
            var totalSize = attributes.Length * sizeof(ushort) + offsets.Length * sizeof(uint) + data.Length;

            var memory = new Memory<byte>(new byte[totalSize]);
            var memorySpan = memory.Span;

            attributes.CastFast<ushort, byte>().CopyTo(memorySpan);
            offsets.CastFast<uint, byte>()
                .CopyTo(memorySpan.SliceFast(attributes.Length * sizeof(ushort)));
            data.CopyTo(memorySpan.SliceFast(attributes.Length * sizeof(ushort) + offsets.Length * sizeof(uint)));

            return new Entry(memory, attributes.Length);
        }
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

        var attributeIndex = entry.Attributes.IndexOf(localCache.Id);
        if (attributeIndex == -1)
            yield break;

        while (attributeIndex < entry.Attributes.Length)
        {
            if (entry.Attributes[attributeIndex] != localCache.Id)
                break;

            var offsets = entry.Offsets;
            var offset = offsets[attributeIndex];
            var length = offsets[attributeIndex + 1] - offset;

            localCache.Serializer.Read(entry.Data.SliceFast((int)offset, (int)length), out var value);
            yield return value;
            attributeIndex++;
        }
    }
}
