using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.UnsafeIterators;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal class Snapshot : ISnapshot, IUnsafeDatomIterable<Snapshot.UnsafeDatomIterator>
{
    private readonly RocksDbSharp.Snapshot _snapshot;
    private readonly Backend _backend;
    private readonly AttributeCache _attributeCache;
    private readonly ReadOptions _readOptions;

    public Snapshot(Backend backend, AttributeCache attributeCache)
    {
        _backend = backend;
        _attributeCache = attributeCache;
        _snapshot = backend.Db!.CreateSnapshot();
        _readOptions =  new ReadOptions()
            .SetSnapshot(_snapshot);
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
    
    [MustDisposeResource]
    public unsafe UnsafeDatomIterator IterateFrom<T>(T from) where T : IUnsafeDatom
    {
        var iterator = _backend.Db!.NewIterator(null, _readOptions);
        iterator.Seek(from.Key, (ulong)from.KeySize);
        return new UnsafeDatomIterator(iterator);
    }
    
    public ref struct UnsafeDatomIterator : IUnsafeIterator
    {
        private readonly Iterator _iterator;
        private bool _isValid;
        private UnsafeDatom _current;

        public UnsafeDatomIterator(RocksDbSharp.Iterator iterator)
        {
            _iterator = iterator;
        }
        public unsafe void Next()
        {
            _iterator.Next();
            _isValid = _iterator.Valid();

            if (!_isValid) 
                return;
            
            _current._key = (byte*)Native.Instance.rocksdb_iter_key(_iterator.Handle, out var keyLength);
            _current._keySize = (int)keyLength;
        }
        
        public bool IsValid => _isValid;
        
        public UnsafeDatom Current => _current;
        
        public ReadOnlySpan<byte> CurrentExtraValue => _iterator!.GetValueSpan();

        public void Dispose()
        {
            _iterator.Dispose();
        }
    }

}

