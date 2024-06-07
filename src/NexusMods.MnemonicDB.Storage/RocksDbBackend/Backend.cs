using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.Paths;
using RocksDbSharp;
using IWriteBatch = NexusMods.MnemonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public class Backend(AttributeRegistry registry) : IStoreBackend
{
    private readonly ColumnFamilies _columnFamilies = new();
    private readonly Dictionary<IndexType, IRocksDbIndex> _indexes = new();
    private readonly Dictionary<IndexType, IRocksDBIndexStore> _stores = new();
    private RocksDb? _db = null!;

    public IWriteBatch CreateBatch()
    {
        return new Batch(_db!);
    }

    public void DeclareIndex<TComparator>(IndexType name)
        where TComparator : IDatomComparator
    {
        var indexStore = new IndexStore<TComparator>(name.ToString(), name);
        _stores.Add(name, indexStore);

        var index = new Index<TComparator>(indexStore);
        _indexes.Add(name, index);
    }

    public IIndex GetIndex(IndexType name)
    {
        return (IIndex)_indexes[name];
    }

    public ISnapshot GetSnapshot()
    {
        return new Snapshot(this, registry);
    }

    public void Init(AbsolutePath location)
    {
        var options = new DbOptions()
            .SetCreateIfMissing()
            .SetCreateMissingColumnFamilies()
            .SetCompression(Compression.Lz4);

        foreach (var (name, store) in _stores)
        {
            var index = _indexes[name];
            store.SetupColumnFamily((IIndex)index, _columnFamilies);
        }

        _db = RocksDb.Open(options, location.ToString(), _columnFamilies);

        foreach (var (name, store) in _stores) store.PostOpenSetup(_db);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    private class Snapshot(Backend backend, AttributeRegistry registry) : ISnapshot
    {
        private readonly RocksDbSharp.Snapshot _snapshot = backend._db!.CreateSnapshot();

        public IndexSegment Datoms(SliceDescriptor descriptor)
        {
            var reverse = descriptor.IsReverse;
            var from = reverse ? descriptor.To : descriptor.From;
            var to = reverse ? descriptor.From : descriptor.To;

            var options = new ReadOptions()
                .SetSnapshot(_snapshot)
                .SetIterateLowerBound(from.RawSpan.ToArray())
                .SetIterateUpperBound(to.RawSpan.ToArray());

            using var builder = new IndexSegmentBuilder(registry);

            using var iterator = backend._db!.NewIterator(backend._stores[descriptor.Index].Handle, options);
            if (reverse)
                iterator.SeekToLast();
            else
                iterator.SeekToFirst();

            using var writer = new PooledMemoryBufferWriter(128);

            while (iterator.Valid())
            {
                writer.Reset();
                writer.Write(iterator.GetKeySpan());

                if (writer.Length >= KeyPrefix.Size + 1)
                {
                    var tag = (ValueTags)writer.GetWrittenSpan()[KeyPrefix.Size];
                    if (tag == ValueTags.HashedBlob)
                    {
                        writer.Write(iterator.GetValueSpan());
                    }
                }

                builder.Add(writer.WrittenMemory.Span);

                if (reverse)
                    iterator.Prev();
                else
                    iterator.Next();
            }
            return builder.Build();
        }

        public IEnumerable<IndexSegment> DatomsChunked(SliceDescriptor descriptor, int chunkSize)
        {
            var reverse = descriptor.IsReverse;
            var from = reverse ? descriptor.To : descriptor.From;
            var to = reverse ? descriptor.From : descriptor.To;

            var options = new ReadOptions()
                .SetSnapshot(_snapshot)
                .SetIterateLowerBound(from.RawSpan.ToArray())
                .SetIterateUpperBound(to.RawSpan.ToArray());

            using var builder = new IndexSegmentBuilder(registry);

            using var iterator = backend._db!.NewIterator(backend._stores[descriptor.Index].Handle, options);
            if (reverse)
                iterator.SeekToLast();
            else
                iterator.SeekToFirst();

            using var writer = new PooledMemoryBufferWriter(128);

            while (iterator.Valid())
            {
                writer.Reset();
                writer.Write(iterator.GetKeySpan());

                if (writer.Length >= KeyPrefix.Size + 1)
                {
                    var tag = (ValueTags)writer.GetWrittenSpan()[KeyPrefix.Size];
                    if (tag == ValueTags.HashedBlob)
                    {
                        writer.Write(iterator.GetValueSpan());
                    }
                }

                builder.Add(writer.WrittenMemory.Span);

                if (builder.Count == chunkSize)
                {
                    yield return builder.Build();
                    builder.Reset();
                }

                if (reverse)
                    iterator.Prev();
                else
                    iterator.Next();
            }
            yield return builder.Build();
        }



    }
}
