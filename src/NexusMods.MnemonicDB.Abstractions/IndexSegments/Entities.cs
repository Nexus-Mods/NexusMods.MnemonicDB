using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A subview of an IndexSegment that returns a specific entity type
/// </summary>
[PublicAPI]
public readonly struct Entities<TInner, TEntity>(TInner ids, IDb db) :
    IReadOnlyCollection<TEntity>
    where TEntity : IReadOnlyModel<TEntity>
    where TInner : IIndexSegment<EntityId>
{
    /// <summary>
    /// Gets the entity at the given index
    /// </summary>
    public TEntity this[int idx] => TEntity.Create(db, ids[idx]);

    /// <summary>
    /// The number of entities in the collection
    /// </summary>
    public int Count => ids.Count;

    /// <summary>
    /// Returns an enumerator.
    /// </summary>
    public Enumerator GetEnumerator() => new(db, ids);
    IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Enumerator.
    /// </summary>
    public struct Enumerator : IEnumerator<TEntity>
    {
        private readonly IDb _db;
        private readonly TInner _ids;
        private int _index;

        internal Enumerator(IDb db, TInner ids)
        {
            _db = db;
            _ids = ids;
        }

        /// <inheritdoc/>
        public TEntity Current { get; private set; } = default!;
        object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_index >= _ids.Count) return false;

            Current = TEntity.Create(_db, _ids[_index++]);
            return true;
        }

        /// <inheritdoc/>
        public void Reset() => _index = 0;

        /// <inheritdoc/>
        public void Dispose() { }
    }
}

/// <summary>
/// A wrapper around EntityIds that auto-creates the given ReadModel on-the-fly
/// </summary>
/// <typeparam name="TModel"></typeparam>
public readonly struct Entities<TModel> : IReadOnlyCollection<TModel>
where TModel : IReadOnlyModel<TModel>
{
    private readonly EntityIds _ids;
    private readonly IDb _db;

    /// <summary>
    /// A wrapper around EntityIds that auto-creates the given ReadModel on-the-fly
    /// </summary>
    public Entities(EntityIds ids, IDb db)
    {
        _ids = ids;
        _db = db;
    }

    /// <inheritdoc />
    public int Count => _ids.Count;
    
    /// <summary>
    /// Get the entity ids in this collection
    /// </summary>
    public EntityIds EntityIds => _ids;

    /// <summary>
    /// Returns an enumerator.
    /// </summary>
    public Enumerator GetEnumerator() => new(_db, _ids);
    IEnumerator<TModel> IEnumerable<TModel>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Entities<{typeof(TModel).FullName ?? typeof(TModel).Name}>({Count})";
    }

    /// <summary>
    /// Enumerator.
    /// </summary>
    public struct Enumerator : IEnumerator<TModel>
    {
        private readonly EntityIds _ids;
        private readonly IDb _db;
        private int _index;

        internal Enumerator(IDb db, EntityIds ids)
        {
            _db = db;
            _ids = ids;
        }

        /// <inheritdoc/>
        public TModel Current { get; private set; } = default!;

        object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_index >= _ids.Count) return false;
            var id = _ids[_index++];

            Current = TModel.Create(_db, id);
            return true;
        }

        /// <inheritdoc/>
        public void Reset() => _index = 0;

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
