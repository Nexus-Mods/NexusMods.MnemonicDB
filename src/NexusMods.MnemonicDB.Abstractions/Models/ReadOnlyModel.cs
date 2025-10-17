using System;
using System.Collections;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// An entity is a reference to the attributes of a specific EnityId. Think of this as a hashmap
/// of attributes, or a row in a database table.
/// </summary>
public readonly struct ReadOnlyModel : IHasIdAndEntitySegment, IEnumerable<ResolvedDatom>
{
    private readonly IDb _db;
    private readonly EntityId _id;
    private readonly Datoms _segment;

    /// <summary>
    /// An entity is a reference to the attributes of a specific EnityId. Think of this as a hashmap
    /// of attributes, or a row in a database table.
    /// </summary>
    public ReadOnlyModel(IDb db, EntityId id)
    {
        _db = db;
        _id = id;
        throw new NotImplementedException();
        //_segment = db.Get(id);
    }

    /// <inheritdoc />
    public EntityId Id => _id;

    /// <inheritdoc />
    public IDb Db => _db;

    /// <inheritdoc />
    public IEnumerator<ResolvedDatom> GetEnumerator()
    {
        var segment = EntitySegment;
        var resolver = Db.Connection.AttributeResolver;
        foreach (var datom in segment)
        {
            yield return resolver.Resolve(datom);
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
    public Datoms EntitySegment => _segment;
}
