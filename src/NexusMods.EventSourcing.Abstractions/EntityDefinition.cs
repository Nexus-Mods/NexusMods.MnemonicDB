using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Defines the metadata about an entity
/// </summary>
/// <param name="Id"></param>
/// <param name="EntityType"></param>
/// <param name="Attributes"></param>
public record EntityDefinition(UInt128 Id, Type EntityType, EntityAttributeDefinition[] Attributes);

/// <summary>
/// Defines the metadata about an entity's attribute
/// </summary>
/// <param name="Name"></param>
/// <param name="NativeType"></param>
public record EntityAttributeDefinition(string Name, Type NativeType);
