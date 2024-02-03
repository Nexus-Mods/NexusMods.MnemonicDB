using System;

namespace NexusMods.EventSourcing.BuiltInEntities;

public record class AttributeDefinition
{

    public uint Id { get; init; }

    public string Name { get; init; } = "";

    public UInt128 EntityDefinition { get; init; }

}
