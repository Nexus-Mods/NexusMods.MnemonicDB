using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal class Snapshot : ISnapshot
{
    /// <summary>
    /// The backend, needed to create iterators
    /// </summary>
    private readonly Backend _backend;

    /// <summary>
    /// The read options, pre-populated with the snapshot
    /// </summary>
    private readonly ReadOptions _readOptions;

    private readonly AttributeCache _attributeCache;
    
    /// <summary>
    /// We keep this here, so that it's not finalized while we're using it
    /// </summary>
    private readonly RocksDbSharp.Snapshot _snapshot;

    public Snapshot(Backend backend, AttributeCache attributeCache, ReadOptions readOptions, RocksDbSharp.Snapshot snapshot)
    {
        _backend = backend;
        _attributeCache = attributeCache;
        _readOptions = readOptions;
        _snapshot = snapshot;
    }

    public IndexSegment Datoms(SliceDescriptor descriptor)
    {
        var reverse = descriptor.IsReverse;
        var from = reverse ? descriptor.To : descriptor.From;
        var to = reverse ? descriptor.From : descriptor.To;
        
        using var builder = new IndexSegmentBuilder(_attributeCache);

        using var iterator = _backend.Db!.NewIterator(null, _readOptions);
        if (!reverse)
            iterator.Seek(from.ToArray());
        else
            iterator.SeekForPrev(to.ToArray());

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

            var curDatom = new Datom(writer.WrittenMemory);
            
            if (!reverse)
            {
                if (GlobalComparer.Compare(curDatom, to) > 0)
                    break;
            }
            else
            {
                if (GlobalComparer.Compare(curDatom, from) < 0)
                    break;
            }

            builder.Add(writer.WrittenMemory.Span);

            if (!reverse)
                iterator.Next();
            else
                iterator.Prev();
        }
        return builder.Build();
    }

    public IEnumerable<IndexSegment> DatomsChunked(SliceDescriptor descriptor, int chunkSize)
    {
        var reverse = descriptor.IsReverse;
        var from = reverse ? descriptor.To : descriptor.From;
        var to = reverse ? descriptor.From : descriptor.To;
        
        using var builder = new IndexSegmentBuilder(_attributeCache);

        using var iterator = _backend.Db!.NewIterator(null, _readOptions);
        if (!reverse)
            iterator.Seek(from.ToArray());
        else
            iterator.SeekForPrev(to.ToArray());

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
            
            var curDatom = new Datom(writer.WrittenMemory);
            
            if (!reverse)
            {
                if (GlobalComparer.Compare(curDatom, to) > 0)
                    break;
            }
            else
            {
                if (GlobalComparer.Compare(curDatom, from) < 0)
                    break;
            }
            
            builder.Add(writer.WrittenMemory.Span);

            if (builder.Count == chunkSize)
            {
                yield return builder.Build();
                builder.Reset();
            }

            if (!reverse)
                iterator.Next();
            else
                iterator.Prev();
        }
        if (builder.Count > 0) 
            yield return builder.Build();
    }
}
