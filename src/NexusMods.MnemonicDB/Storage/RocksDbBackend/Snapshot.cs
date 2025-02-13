using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal class Snapshot : ISnapshot
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


    public RawDatomEnumerable ForwardDatoms(RawDatom from, RawDatom to)
    {
        return new RawDatomEnumerable(from, to);
    }

    public ref struct RawDatomEnumerable(Snapshot snapshot, RawDatom from, RawDatom to)
    {
        public RawDatomEnumerator GetEnumerator()
        {
            return new RawDatomEnumerator(snapshot._snapshot, from, to);
        }
    }

    public ref struct RawDatomEnumerator
    {
        private readonly RawDatom _from;
        private readonly RawDatom _to;
        private readonly Snapshot _snapshot;
        private Iterator? _iterator;
        
        private RawDatom _current;

        public RawDatomEnumerator(Snapshot snapshot, RawDatom from, RawDatom to)
        {
            _snapshot = snapshot;
            _from = from;
            _to = to;
        }
        public bool MoveNext()
        {
            if (_iterator == null)
                return SeekStart();

            _iterator.Next();
            if (!_iterator.Valid())
                return false;

            ReadPointers();
            return Current.CompareTo(_to) <= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private unsafe void ReadPointers()
        {
            _current._key = (byte*)Native.Instance.rocksdb_iter_key(_iterator!.Handle, out var keyLength);
            _current._keySize = (int)keyLength;
            Debug.Assert(keyLength >= KeyPrefix.Size, "keyLength >= KeyPrefix.Size");
        }

        private unsafe bool SeekStart()
        {
            _iterator = _snapshot._backend.Db!.NewIterator(null, _snapshot._readOptions);
            Native.Instance.rocksdb_iter_seek(_iterator.Handle, (IntPtr)_from._key, (UIntPtr)_from._keySize);
            ReadPointers();
            return Current.CompareTo(_to) >= 0;
        }

        public RawDatom Current => _current;
        
        public ReadOnlySpan<byte> CurrentValue => _iterator!.GetValueSpan();
        
    }
}

