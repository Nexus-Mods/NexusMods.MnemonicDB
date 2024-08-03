using System.Collections;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// An entity is a reference to the attributes of a specific EnityId. Think of this as a hashmap
/// of attributes, or a row in a database table.
/// </summary>
public readonly struct ReadOnlyModel : IHasIdAndIndexSegment, IEnumerable<IReadDatom>
{
    private readonly IDb _db;
    private readonly EntityId _id;
    private readonly IndexSegment _segment;

    /// <summary>
    /// An entity is a reference to the attributes of a specific EnityId. Think of this as a hashmap
    /// of attributes, or a row in a database table.
    /// </summary>
    public ReadOnlyModel(IDb db, EntityId id)
    {
        _db = db;
        _id = id;
        _segment = db.Get(id);
    }

    /// <inheritdoc />
    public EntityId Id => _id;

    /// <inheritdoc />
    public IDb Db => _db;

    /// <inheritdoc />
    public IEnumerator<IReadDatom> GetEnumerator()
    {
        var segment = IndexSegment;
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

    /// <inheritdoc />
    public override string ToString()
    {
        return $"ReadOnlyModel<{Id.Value:x}>";
    }

    /// <inheritdoc />
    public IndexSegment IndexSegment => _segment;
}
