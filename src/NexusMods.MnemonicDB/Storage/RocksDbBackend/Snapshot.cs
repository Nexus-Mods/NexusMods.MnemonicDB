using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using Reloaded.Memory.Extensions;
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

        using var iterator = backend.Db!.NewIterator(null, options);
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

        using var builder = new IndexSegmentBuilder(attributeCache);

        using var iterator = backend.Db!.NewIterator(null, options);
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

    public IEnumerable<RefDatom> RefDatoms(SliceDescriptor descriptor)
    {
        return new RefEnumerable(backend, this, descriptor);
    }

    private class RefEnumerable(Backend backend, Snapshot snapshot, SliceDescriptor sliceDescriptor) : IEnumerable<RefDatom>
    {
        public IEnumerator<RefDatom> GetEnumerator()
        {
            return new RefEnumerator(backend, snapshot, sliceDescriptor);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class RefEnumerator : IEnumerator<RefDatom>
    {
        private readonly Snapshot _snapshot;
        private readonly SliceDescriptor _sliceDescriptor;
        private readonly bool _reverse;
        private readonly Datom _from;
        private readonly Datom _to;
        private readonly ReadOptions _options;
        private RocksDbSharp.Iterator? _iterator;
        private readonly Backend _backend;

        public RefEnumerator(Backend backend, Snapshot snapshot, SliceDescriptor descriptor)
        {
            _backend = backend;
            _snapshot = snapshot;
            _sliceDescriptor = descriptor;
            
            _reverse = descriptor.IsReverse;
            _from = _reverse ? descriptor.To : descriptor.From;
            _to = _reverse ? descriptor.From : descriptor.To;

            _options = new ReadOptions()
                .SetSnapshot(snapshot._snapshot)
                .SetIterateLowerBound(_from.ToArray())
                .SetIterateUpperBound(_to.ToArray());
        }

        public void Dispose()
        {
            _iterator?.Dispose();
        }

        public bool MoveNext()
        {
            if (_iterator == null)
            {
                _iterator = _backend.Db!.NewIterator(null, _options);
                if (_reverse)
                    _iterator.SeekToLast();
                else
                    _iterator.SeekToFirst();
            }
            else
            {
                if (_reverse)
                    _iterator.Prev();
                else
                    _iterator.Next();
            }

            return _iterator.Valid();
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        RefDatom IEnumerator<RefDatom>.Current
        {
            get
            {
                var key = _iterator!.GetKeySpan();
                var keyPrefix = KeyPrefix.Read(key);
                if (keyPrefix.ValueTag == ValueTag.HashedBlob)
                    throw new InvalidOperationException("RefDatom enumeration should only return RefDatoms");

                return new RefDatom(keyPrefix, key.SliceFast(KeyPrefix.Size));
            }
        }

        object? IEnumerator.Current => throw new NotSupportedException();
    }
    
}
