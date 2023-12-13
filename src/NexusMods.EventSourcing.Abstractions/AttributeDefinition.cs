using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// An attribute definition for an entity.
/// </summary>
/// <param name="attrName"></param>
/// <typeparam name="TOwner"></typeparam>
/// <typeparam name="TType"></typeparam>
public class AttributeDefinition<TOwner, TType>(string attrName) : AScalarAttribute<TOwner, TType>(attrName)
where TOwner : IEntity
{
    /// <summary>
    /// Gets the value of the attribute for the given entity.
    /// </summary>
    /// <param name="owner"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TType Get(TOwner owner) => (TType)owner.Context.GetAccumulator<TOwner, AttributeDefinition<TOwner, TType>>(owner.Id, this).Get();
}
