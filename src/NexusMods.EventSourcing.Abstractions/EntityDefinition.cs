using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Records the entity type, UUID and revision, for use in the DI container.
/// </summary>
/// <param name="Type"></param>
/// <param name="UUID"></param>
/// <param name="Revision"></param>
public record EntityDefinition(Type Type, UInt128 UUID, ushort Revision, EntityId? SingletonId);
