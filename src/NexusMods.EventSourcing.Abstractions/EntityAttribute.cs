using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A annotation for an entity to specify the entity's attributes
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EntityAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance of <see cref="EntityAttribute"/>
    /// </summary>
    /// <param name="guid"></param>
    public EntityAttribute(string guid)
    {
        Id = guid.GuidStringToUInt128();

    }

    /// <summary>
    /// The unique identifier for the entity
    /// </summary>
    public UInt128 Id { get; set; }
}
