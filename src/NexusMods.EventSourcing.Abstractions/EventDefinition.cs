using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A record that defines an event and the unique GUID that identifies it.
/// </summary>
/// <param name="Guid"></param>
/// <param name="Type"></param>
public record EventDefinition(Guid Guid, Type Type);
