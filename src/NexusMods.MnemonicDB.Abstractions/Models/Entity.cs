using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// An entity is a reference to the attributes of a specific EnityId. Think of this as a hashmap
/// of attributes, or a row in a database table.
/// </summary>
public class Entity : IEntityWithTx
{
    /// <summary>
    /// Constructs an entity, and attaches it to a transaction
    /// </summary>
    /// <param name="tx"></param>
    protected Entity(ITransaction tx)
    {
        // This looks like it's never null, but the framework will force-inject a null here when constructing
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (tx != null)
        {
            Tx = tx;
            Id = tx.TempId();
            Db = null!;
        }
        else
        {
            Id = EntityId.MinValue;
            Tx = null;
            Db = null!;
        }

    }

    /// <summary>
    /// The transaction the entity is currently attached to (if any)
    /// </summary>
    public ITransaction? Tx { get; set; }

    /// <summary>
    /// Get the reverse of a relationship.
    /// </summary>
    protected Entities<EntityIds, TModel> GetReverse<TModel>(Attribute<EntityId, ulong> attribute)
        where TModel : IEntity
    {
        return Db.GetReverse<TModel>(Id, attribute);
    }

    /// <summary>
    /// The id of the entity.
    /// </summary>
    public EntityId Id { get; init; }

    /// <summary>
    /// The database the entity is stored in.
    /// </summary>
    public IDb Db { get; init; }

    /// <inheritdoc />
    public bool Contains(IAttribute attribute)
        => attribute.IsIn(Db, Id);

    /// <inheritdoc />
    public static IEntity From(IDb db, EntityId id)
    {
        return new Entity(null!) { Db = db, Id = id };
    }

    /// <summary>
    /// Creates a new entity of the given type and attaches it to the given transaction.
    /// </summary>
    public static T New<T>() where T : Entity
    {
        return (T) Activator.CreateInstance(typeof(T), null)!;
    }

    /// <inheritdoc />
    public IEnumerator<Datom> GetEnumerator()
    {
        return Db.GetSegment(Id)
            .GetEnumerator();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var fullName = GetType().FullName!;
        var dotIdx = fullName.LastIndexOf(".");
        var name = fullName[(dotIdx+1)..];
        return $"{name}<{Id.Value:x}>";
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
