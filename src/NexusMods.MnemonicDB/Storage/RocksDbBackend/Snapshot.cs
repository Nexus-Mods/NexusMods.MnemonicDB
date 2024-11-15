using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Iterators;
using NexusMods.MnemonicDB.Abstractions.Query;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal class Snapshot : ISnapshot
{
    private readonly RocksDbSharp.Snapshot _snapshot;
    private readonly ReadOptions _readOptions;
    private readonly Backend _backend;
    private readonly AttributeCache _attributeCache;

    public Snapshot(Backend backend, AttributeCache attributeCache)
    {
        _backend = backend;
        _attributeCache = attributeCache;
        _snapshot = backend.Db!.CreateSnapshot();
        _readOptions = new ReadOptions().SetSnapshot(_snapshot);
    }

    public IndexSegment Datoms(SliceDescriptor descriptor)
    {
        var reverse = descriptor.IsReverse;
        var from = reverse ? descriptor.To : descriptor.From;
        var to = reverse ? descriptor.From : descriptor.To;

        var options = new ReadOptions()
            .SetSnapshot(_snapshot)
            .SetIterateLowerBound(from.ToArray())
            .SetIterateUpperBound(to.ToArray());

        using var builder = new IndexSegmentBuilder(_attributeCache);

        using var iterator = _backend.Db!.NewIterator(null, options);
        if (reverse)
            iterator.SeekToLast();
        else
            iterator.SeekToFirst();

        using var writer = new PooledMemoryBufferWriter(128);

        while (iterator.Valid())
        {
            writer.Reset();
            writer.Write(iterator.GetKeySpan());

            if (writer.Length >= KeyPrefix.Size)
            {
                var prefix = KeyPrefix.Read(writer.GetWrittenSpan());
                if (prefix.ValueTag == ValueTag.HashedBlob)
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
            .SetIterateLowerBound(from.ToArray())
            .SetIterateUpperBound(to.ToArray());

        using var builder = new IndexSegmentBuilder(_attributeCache);

        using var iterator = _backend.Db!.NewIterator(null, options);
        if (reverse)
            iterator.SeekToLast();
        else
            iterator.SeekToFirst();

        using var writer = new PooledMemoryBufferWriter(128);

        while (iterator.Valid())
        {
            writer.Reset();
            writer.Write(iterator.GetKeySpan());

            if (writer.Length >= KeyPrefix.Size)
            {
                var prefix = KeyPrefix.Read(writer.GetWrittenSpan());
                if (prefix.ValueTag == ValueTag.HashedBlob)
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
        if (builder.Count > 0) 
            yield return builder.Build();
    }

    public unsafe void Fold<TFn, TDesc>(TDesc descriptor, ref TFn fn) 
        where TDesc : IRefSliceDescriptor, allows ref struct 
        where TFn : IFolder<RefDatom>, allows ref struct
    {
        using var iterator = _backend.Db!.NewIterator(null, _readOptions);
        iterator.Seek(descriptor.LowerBound);
        fn.Start();
        var upperLimitSize = descriptor.UpperBound.Length;
        fixed (byte* upperLimitPtr = descriptor.UpperBound)
        {
            while (iterator.Valid())
            {
                var keyPtr = Native.Instance.rocksdb_iter_key(iterator.Handle, out var keyLen);
                if (GlobalComparer.Compare((byte*)keyPtr, (int)keyLen, upperLimitPtr, upperLimitSize) > 0)
                    break;

                var current = new RefDatom(iterator.GetKeySpan());
                if (!fn.Add(current))
                    break;
                iterator.Next();
            }
        }
    }
}
