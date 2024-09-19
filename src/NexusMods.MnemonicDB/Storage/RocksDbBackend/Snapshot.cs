using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal class Snapshot(Backend backend, AttributeCache attributeCache) : ISnapshot
{
    private readonly RocksDbSharp.Snapshot _snapshot = backend.Db!.CreateSnapshot();

    public IndexSegment Datoms(SliceDescriptor descriptor)
    {
        var reverse = descriptor.IsReverse;
        var from = reverse ? descriptor.To : descriptor.From;
        var to = reverse ? descriptor.From : descriptor.To;

        var options = new ReadOptions()
            .SetSnapshot(_snapshot)
            .SetIterateLowerBound(from.ToArray())
            .SetIterateUpperBound(to.ToArray());

        using var builder = new IndexSegmentBuilder(attributeCache);

        using var iterator = backend.Db!.NewIterator(backend.Stores[descriptor.Index].Handle, options);
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
                if (prefix.ValueTag == ValueTags.HashedBlob)
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

        using var builder = new IndexSegmentBuilder(attributeCache);

        using var iterator = backend.Db!.NewIterator(backend.Stores[descriptor.Index].Handle, options);
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
                if (prefix.ValueTag == ValueTags.HashedBlob)
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
