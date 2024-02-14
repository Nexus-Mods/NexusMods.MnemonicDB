using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A push-based system for building up a IReadModel state
/// </summary>
public interface IReadModelBuilder
{
    /// <summary>
    /// Sets the value of the given attribute to the given value
    /// </summary>
    /// <param name="val"></param>
    /// <typeparam name="TAttribute"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    public void Set(IAttribute attr, ReadOnlySpan<byte> span);

    /// <summary>
    /// Builds the collected data into a IReadModel
    /// </summary>
    /// <returns></returns>
    public IReadModel Build(EntityId id);
}

