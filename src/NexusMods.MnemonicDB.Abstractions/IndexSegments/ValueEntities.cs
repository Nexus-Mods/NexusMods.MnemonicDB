using System.Collections;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A wrapper around Values that auto-creates the given ReadModel on-the-fly
/// </summary>
public readonly struct ValueEntities<TModel> : IReadOnlyCollection<TModel>
    where TModel : IReadOnlyModel<TModel>
{
    private readonly Values<EntityId, EntityId> _values;

    /// <summary>
    /// The database the models are read from
    /// </summary>
    private IDb Db { get; }

    /// <summary>
    /// Creates a new ValueEntities, from the given values, database, and entity id
    /// </summary>
    public ValueEntities(Values<EntityId, EntityId> values, IDb db)
    {
        _values = values;
        Db = db;
    }

    /// <inheritdoc />
    public IEnumerator<TModel> GetEnumerator()
    {
        foreach (var value in _values)
        {
            yield return TModel.Create(Db, value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    /// <inheritdoc />
    public int Count => _values.Count;
}
