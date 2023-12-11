using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Marks a an event as having the given GUID id
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EventIdAttribute : Attribute
{
    /// <summary>
    /// The GUID of the entity type.
    /// </summary>
    public readonly Guid Guid;


    /// <summary>
    /// Creates a new instance of the <see cref="EventIdAttribute"/> class.
    /// </summary>
    /// <param name="guid"></param>
    public EventIdAttribute(string guid)
    {
        Guid = Guid.Parse(guid);
    }
}
