using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// A temporary entity that can be added to a transaction, use this to construct an entity, return it from
/// a function and then add it to the transaction later on. This is useful when you need to create an entity
/// but don't want to pass a transaction around to internal functions.
/// </summary>
public class TempEntity : IEnumerable<(IAttribute, object)>
{
    private readonly List<(IAttribute Attribute, object Value)> _members = new();

    public EntityId? Id { get; set; }

    /// <summary>
    /// Adds an attribute/value to the entity.
    /// </summary>
    public void Add<T>(IAttribute<T> attribute, T value)
    {
        _members.Add((attribute, value!));
    }

    /// <summary>
    /// Adds an attribute/value to the entity.
    /// </summary>
    public void Add(IAttribute<EntityId> attribute, TempEntity value)
    {
        _members.Add((attribute, value));
    }

    /// <summary>
    /// Adds a marker attribute to the entity.
    /// </summary>
    public void Add(MarkerAttribute attr)
    {
        _members.Add((attr, new Null()));
    }

    /// <summary>
    /// Adds the entity and any nested entities to the transaction.
    /// </summary>
    /// <param name="tx"></param>
    public virtual void AddTo(ITransaction tx)
    {
        Id ??= tx.TempId();
        foreach (var (attribute, value) in _members)
        {
            if (value is TempEntity tempEntity)
            {
                tempEntity.AddTo(tx);
                attribute.Add(tx, Id.Value, tempEntity.Id!.Value, false);
            }
            else
            {
                attribute.Add(tx, Id.Value, value, false);
            }
        }
    }

    /// <summary>
    /// Returns true if the entity contains the attribute.
    /// </summary>
    public bool Contains(IAttribute attribute) => _members.Any(x => x.Attribute == attribute);

    /// <summary>
    /// Gets all the values for the given attribute on the entity.
    /// </summary>
    public IEnumerable<TValue> Get<TValue>(IAttribute<TValue> attribute)
    {
        foreach (var (attr, value) in _members)
        {
            if (attr == attribute)
            {
                yield return (TValue) value;
            }
        }
    }

    /// <summary>
    /// Gets the first value for the given attribute on the entity.
    /// </summary>
    public TValue GetFirst<TValue>(IAttribute<TValue> attribute)
    {
        foreach (var (attr, value) in _members)
        {
            if (attr == attribute)
            {
                return (TValue) value;
            }
        }

        throw new KeyNotFoundException($"Entity {Id} does not have attribute {attribute.Id}");
    }

    /// <inheritdoc />
    public IEnumerator<(IAttribute, object)> GetEnumerator()
    {
        return _members.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
