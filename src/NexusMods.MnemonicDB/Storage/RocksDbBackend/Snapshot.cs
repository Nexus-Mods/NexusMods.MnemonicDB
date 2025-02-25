using System;
using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using Reloaded.Memory.Extensions;
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
        using var builder = new IndexSegmentBuilder(_attributeCache);
        builder.AddRange<RefDatomEnumerable, RefDatomEnumerator>(RefDatoms(descriptor));
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
    
    /// <summary>
    /// Get a high performance, ref-based enumerable of datoms
    /// </summary>
    public RefDatomEnumerable RefDatoms(SliceDescriptor descriptor)
    {
        return new(this, descriptor);
    }


    public readonly ref struct RefDatomEnumerable(Snapshot snapshot, SliceDescriptor sliceDescriptor) : IRefDatomEnumerable<RefDatomEnumerator>
    {
        public RefDatomEnumerator GetEnumerator() => new(snapshot, sliceDescriptor);
    }

    public ref struct RefDatomEnumerator : IRefDatomEnumerator
    {
        private readonly Snapshot _snapshot;
        private Iterator? _iterator;
        private readonly bool _reverse;
        private readonly byte[] _from;
        private readonly byte[] _to;
        private ReadOnlySpan<byte> _currentSpan;

        public RefDatomEnumerator(Snapshot snapshot, SliceDescriptor sliceDescriptor)
        {
            _snapshot = snapshot;
            _reverse = sliceDescriptor.IsReverse;
            _from = (_reverse ? sliceDescriptor.To : sliceDescriptor.From).ToArray();
            _to = (_reverse ? sliceDescriptor.From : sliceDescriptor.To).ToArray();
        }
        
        public void Dispose()
        {
            _iterator?.Dispose();
        }

        public bool MoveNext()
        {
            if (_iterator == null)
            {
                _iterator = _snapshot._backend.Db!.NewIterator(null, _snapshot._readOptions);
                if (!_reverse)
                    _iterator.Seek(_from);
                else
                    _iterator.SeekForPrev(_to);
            }
            else
            {
                if (!_reverse)
                    _iterator.Next();
                else
                    _iterator.Prev();
            }
            
            if (_iterator.Valid())
            {
                _currentSpan = _iterator.GetKeySpan();
                if (!_reverse)
                {
                    if (GlobalComparer.Compare(_currentSpan, _to.AsSpan()) > 0)
                        return false;
                }
                else
                {
                    if (GlobalComparer.Compare(_currentSpan, _from.AsSpan()) < 0)
                        return false;
                }
                return true;
            }
            return false;
        }

        public KeyPrefix KeyPrefix => KeyPrefix.Read(_currentSpan);
        public ReadOnlySpan<byte> ValueSpan => _currentSpan.SliceFast(KeyPrefix.Size);

        public ReadOnlySpan<byte> ExtraValueSpan 
        {
            get
            {
                Debug.Assert(KeyPrefix.ValueTag == ValueTag.HashedBlob, "ExtraValueSpan is only valid for HashedBlob values");
                return _iterator!.GetValueSpan();
            }
        }
    }
}
