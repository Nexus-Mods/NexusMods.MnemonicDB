using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A read model is a set of attributes grouped together with a common entity id
/// </summary>
public interface IReadModel
{
    /// <summary>
    /// The unique identifier of the entity in the read model
    /// </summary>
    public EntityId Id { get; }

    /// <summary>
    /// Sets the value of an attribute in the model
    /// </summary>
    /// <param name="model"></param>
    /// <param name="attribute"></param>
    /// <param name="value"></param>
    public void Set(IAttribute attribute, ReadOnlySpan<byte> value);
}
