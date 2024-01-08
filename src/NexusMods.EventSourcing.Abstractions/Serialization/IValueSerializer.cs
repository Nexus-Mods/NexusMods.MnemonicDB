using System;
using System.Linq.Expressions;

namespace NexusMods.EventSourcing.Abstractions.Serialization;

/// <summary>
/// A value serializer.
/// </summary>
public interface IValueSerializer
{
    /// <summary>
    /// The type this serializer is for.
    /// </summary>
    public Type ForType { get; }

    /// <summary>
    /// True if the size of the serialized value is dynamic (changes based on the value).
    /// </summary>
    public bool IsDynamicSize { get; }

}
