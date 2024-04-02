using System;
using System.Collections.Generic;
using NexusMods.MneumonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB.Abstractions.DatomIterators;

/// <summary>
///  A segment of an index, used most often as a cache. For example when an entity is read from the database,
/// the whole entity may be cached in one of these segments for fast access.
/// </summary>
public class IndexSegment
{
    private Memory<byte> _data;
    private Memory<int> _offsets;

    /// <summary>
    /// Construct a new index segment from the given data and offsets
    /// </summary>
    public IndexSegment(ReadOnlySpan<byte> data, ReadOnlySpan<int> offsets)
    {
        _data = data.ToArray();
        _offsets = offsets.ToArray();
    }

    /// <summary>
    /// Get the datom of the given index
    /// </summary>
    public ReadOnlySpan<byte> this[int idx]
    {
        get
        {
            var fromOffset = _offsets.Span[idx];
            return _data.Span.SliceFast(fromOffset, _offsets.Span[idx + 1] - fromOffset);
        }
    }

    /// <summary>
    /// Get the total size of the segment in datoms
    /// </summary>
    public int Count => _offsets.Length - 1;


    /// <summary>
    /// Get an iterator for the index segment
    /// </summary>
    public IndexedDatomSource<TComparator> GetIterator<TComparator>(IAttributeRegistry registry)
        where TComparator : IDatomComparator
    {
        return new IndexedDatomSource<TComparator>(this, registry);
    }

    /// <summary>
    /// An iterator for the index segment
    /// </summary>
    public struct IndexedDatomSource<TComparator>(IndexSegment segment, IAttributeRegistry registry)
        : IDatomSource, IIterator
    where TComparator : IDatomComparator
    {
        private int _offset = 0;

        /// <inheritdoc />
        public void Dispose()
        {

        }

        /// <inheritdoc />
        public IIterator SeekLast()
        {
            _offset = segment.Count - 1;
            return this;
        }

        /// <inheritdoc />
        public IIterator Seek(ReadOnlySpan<byte> datom)
        {
            var low = 0;
            var high = segment.Count - 1;
            while (low <= high)
            {
                var mid = (low + high) / 2;
                var cmp = TComparator.Compare(registry, segment[mid], datom);
                switch (cmp)
                {
                    case < 0:
                        low = mid + 1;
                        break;
                    case > 0:
                        high = mid - 1;
                        break;
                    default:
                    {
                        // If the element at mid - 1 is also equal to datom, continue the search in the lower half
                        if (mid > 0 && TComparator.Compare(registry, segment[mid - 1], datom) == 0)
                        {
                            high = mid - 1;
                        }
                        else
                        {
                            _offset = mid;
                            return this;
                        }

                        break;
                    }
                }
            }

            _offset = low;
            return this;
        }

        /// <inheritdoc />
        public IIterator SeekStart()
        {
            _offset = 0;
            return this;
        }

        /// <inheritdoc />
        public bool Valid => _offset < segment.Count && _offset >= 0;

        /// <inheritdoc />
        public ReadOnlySpan<byte> Current => segment[_offset];

        /// <inheritdoc />
        public IAttributeRegistry Registry => registry;

        /// <inheritdoc />
        public void Next()
        {
            _offset++;
        }

        /// <inheritdoc />
        public void Prev()
        {
            _offset--;
        }
    }
}
