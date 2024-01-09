using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A record that defines an event and the unique GUID that identifies it.
/// </summary>
/// <param name="Id"></param>
/// <param name="Type"></param>
public record EventDefinition(UInt128 Id, Type Type);
