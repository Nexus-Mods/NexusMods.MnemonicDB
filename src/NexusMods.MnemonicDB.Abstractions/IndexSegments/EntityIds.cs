using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A subview of an IndexSegment that returns the entity ids of the segment
/// </summary>
[PublicAPI]
public readonly struct EntityIds(Memory<EntityId> segment) :
    IReadOnlyCollection<EntityId>, IIndexSegment<EntityId>
{
    /// <summary>
    /// Gets the value at the given location
    /// </summary>
    public EntityId this[int idx] => segment.Span[idx];

    /// <summary>
    /// Returns the number of items in the collection
    /// </summary>
    public int Count => segment.Length;

    /// <summary>
    /// Converts the view to an array
    /// </summary>
    public EntityId[] ToArray()
    {
        var arr = GC.AllocateUninitializedArray<EntityId>(Count);
        for (var i = 0; i < Count; i++)
        {
            arr[i] = this[i];
        }
        return arr;
    }

    /// <summary>
    /// Returns an enumerator.
    /// </summary>
    public Enumerator GetEnumerator() => new(segment);
    IEnumerator<EntityId> IEnumerable<EntityId>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Creates a view of these Ids that auto-casts every Id into a model of the given model type
    /// </summary>
    public Entities<TModel> AsModels<TModel>(IDb db)
    where TModel : IReadOnlyModel<TModel>
    {
        return new Entities<TModel>(this, db);
    }

    /// <summary>
    /// Enumerator.
    /// </summary>
    public struct Enumerator : IEnumerator<EntityId>
    {
        private readonly Memory<EntityId> _segment;
        private int _index;
        private readonly int _max;

        internal Enumerator(Memory<EntityId> segment)
        {
            _max = segment.Length;
            _segment = segment;
            _index = 0;
        }

        /// <inheritdoc/>
        public EntityId Current { get; private set; } = default!;
        object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_index >= _max) return false;
            Current = _segment.Span[_index];
            _index += 1;
            return true;
        }

        /// <inheritdoc/>
        public void Reset() => _index = 0;

        /// <inheritdoc/>
        public void Dispose() { }
    }
}
