using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// This is the context interface passed to event handlers, it allows the handler to attach new entities to the context
/// </summary>
public interface IEventContext
{
    /// <summary>
    /// Gets the accumulator for the given attribute definition, if the accumulator does not exist it will be created. If
    /// the context is not setup for this entityId then false will be returned and the accumulator should be ignored.
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="attributeDefinition"></param>
    /// <param name="accumulator"></param>
    /// <typeparam name="TOwner"></typeparam>
    /// <typeparam name="TAttribute"></typeparam>
    /// <typeparam name="TAccumulator"></typeparam>
    /// <returns></returns>
    public bool GetAccumulator<TOwner, TAttribute, TAccumulator>(EntityId<TOwner> entityId, TAttribute attributeDefinition, [NotNullWhen(true)] out TAccumulator accumulator)
        where TAttribute : IAttribute<TAccumulator>
        where TAccumulator : IAccumulator
        where TOwner : IEntity;
}
