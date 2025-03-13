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
public readonly struct EntityIds : ISegment<EntityId>, IReadOnlyCollection<EntityId>
{
    /// <summary>
    /// Gets the value at the given location
    /// </summary>
    public EntityId this[int idx] => this.GetValues1<EntityIds, EntityId>()[idx];
    
    /// <summary>
    /// A span of all the values in the segment
    /// </summary>
    public ReadOnlySpan<EntityId> Span => this.GetValues1<EntityIds, EntityId>();

    /// <inheritdoc />
    public required ReadOnlyMemory<byte> Data { get; init; }
    
    /// <summary>
    /// Builds the segment of this type from the given builder
    /// </summary>
    public static Memory<byte> Build(in IndexSegmentBuilder builder)
    {
        return builder.Build<EntityId>();
    }

    /// <summary>
    /// Returns the number of items in the collection
    /// </summary>
    public int Count => this.GetCount();

    /// <summary>
    /// Converts the view to an array
    /// </summary>
    public EntityId[] ToArray() => 
        this.GetValues1<EntityIds, EntityId>().ToArray();

    /// <summary>
    /// Returns an enumerator.
    /// </summary>
    public Enumerator GetEnumerator() => new(this);
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
        private readonly EntityIds _segment;
        private int _index;

        internal Enumerator(EntityIds segment)
        {
            _segment = segment;
            _index = 0;
        }

        /// <inheritdoc/>
        public EntityId Current { get; private set; } = default!;
        object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            var span = _segment.GetValues1<EntityIds, EntityId>();
            if (_index >= span.Length) return false;
            Current = _segment[_index];
            _index += 1;
            return true;
        }

        /// <inheritdoc/>
        public void Reset() => _index = 0;

        /// <inheritdoc/>
        public void Dispose() { }
    }
}
