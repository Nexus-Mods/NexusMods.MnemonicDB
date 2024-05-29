using System.Collections;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// An entity is a reference to the attributes of a specific EnityId. Think of this as a hashmap
/// of attributes, or a row in a database table.
/// </summary>
public class ReadOnlyModel(IDb db, EntityId id) : IHasEntityIdAndDb, IEnumerable<IReadDatom>
{
    /// <inheritdoc />
    public EntityId Id => id;

    /// <inheritdoc />
    public IDb Db => db;

    /// <inheritdoc />
    public IEnumerator<IReadDatom> GetEnumerator()
    {
        var segment = db.Get(id);
        for (var i = 0; i < segment.Count; i++)
        {
            yield return segment[i].Resolved;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Looks for the given attribute in the entity
    /// </summary>
    public bool Contains(IAttribute attribute)
    {
        foreach (var datom in this)
        {
            if (datom.A == attribute)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Override this to provide a custom model name
    /// </summary>
    protected virtual string ModelName => GetType().Name;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{ModelName}<{Id.Value:x}>";
    }
}
