using System;
using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal sealed class Snapshot : ISnapshot
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
    // ReSharper disable once NotAccessedField.Local
    private readonly RocksDbSharp.Snapshot _snapshot;

    public Snapshot(Backend backend, AttributeCache attributeCache, ReadOptions readOptions, RocksDbSharp.Snapshot snapshot)
    {
        _backend = backend;
        _attributeCache = attributeCache;
        _readOptions = readOptions;
        _snapshot = snapshot;
    }

    public IndexSegment Datoms<TDescriptor>(TDescriptor descriptor) where TDescriptor : ISliceDescriptor
    {
        using var builder = new IndexSegmentBuilder(_attributeCache);
        builder.AddRange<RefDatomEnumerable<TDescriptor>, RefDatomEnumerator<TDescriptor>>(RefDatoms(descriptor));
        return builder.Build();
    }

    public IEnumerable<IndexSegment> DatomsChunked(SliceDescriptor descriptor, int chunkSize)
    {
        using var builder = new IndexSegmentBuilder(_attributeCache);
        using var enumerable = RefDatoms(descriptor).GetEnumerator();
        while (enumerable.MoveNext())
        {
            builder.AddCurrent(enumerable);
            if (builder.Count == chunkSize)
            {
                yield return builder.Build();
                builder.Reset();
            }
        }
        if (builder.Count > 0)
            yield return builder.Build();
    }
    
    /// <summary>
    /// Get a high performance, ref-based enumerable of datoms
    /// </summary>
    public RefDatomEnumerable<TDescriptor> RefDatoms<TDescriptor>(TDescriptor descriptor)
    where TDescriptor : ISliceDescriptor
    {
        return new(this, descriptor);
    }


    public readonly ref struct RefDatomEnumerable<TDescriptor>(Snapshot snapshot, TDescriptor sliceDescriptor) 
        : IRefDatomEnumerable<RefDatomEnumerator<TDescriptor>>
    where TDescriptor : ISliceDescriptor
    {
        public RefDatomEnumerator<TDescriptor> GetEnumerator() => new(snapshot, sliceDescriptor);
    }

    public unsafe struct RefDatomEnumerator<TDescriptor> : IRefDatomEnumerator
    where TDescriptor : ISliceDescriptor
    {
        private readonly Snapshot _snapshot;
        private Iterator? _iterator;
        private readonly TDescriptor _descriptor;
        private IntPtr _currentKey;
        private UIntPtr _currentKeyLength;

        public RefDatomEnumerator(Snapshot snapshot, TDescriptor sliceDescriptor)
        {
            _descriptor = sliceDescriptor;
            _snapshot = snapshot;
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
                _descriptor.Reset(new IteratorWrapper(_iterator));
            }
            else
            {
                _descriptor.MoveNext(new IteratorWrapper(_iterator));
            }
            
            if (_iterator.Valid())
            {
                _currentKey = Native.Instance.rocksdb_iter_key(_iterator.Handle, out _currentKeyLength);
                var currentSpan = new ReadOnlySpan<byte>((void*)_currentKey, (int)_currentKeyLength);
                return _descriptor.ShouldContinue(currentSpan);
            }
            return false;
        }

        public KeyPrefix KeyPrefix => *(KeyPrefix*)_currentKey;
        public ReadOnlySpan<byte> ValueSpan => new((void*)(_currentKey + KeyPrefix.Size), (int)_currentKeyLength - KeyPrefix.Size);

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
