using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A instance of an attribute
/// </summary>
public record AttributeDefinition
{
    /// <summary>
    /// The unique identifier of the entity this attribute is associated with
    /// </summary>
    public required UInt128 EntityTypeId { get; init; }

    /// <summary>
    /// The name of the attribute
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The DB attribute ID, which also happens to be the EntityId of the attribute's metadata entity
    /// in the database.
    /// </summary>
    public ulong DbId { get; set; }

    /// <summary>
    /// The attribute type information
    /// </summary>
    public required IAttributeType AttributeType { get; init; }
}
