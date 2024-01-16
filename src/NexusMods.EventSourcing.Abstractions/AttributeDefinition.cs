using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A record containing an attribute definitions
/// </summary>
/// <param name="Attribute"></param>
/// <param name="Owner"></param>
/// <param name="Indexed"></param>
public record AttributeDefinition(IAttribute Attribute, Type Owner, string Name, bool Indexed);
