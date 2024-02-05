using System;

namespace NexusMods.EventSourcing.Abstractions.BuiltinEntities;

/// <summary>
/// A specialized entity that contains metadata about attributes
/// </summary>
[Entity("5015072B-1B8F-48AD-8BC4-DDFD2EC9A00B")]
public class DbRegisteredAttribute
{
    public static UInt128 StaticUniqueId = "5015072B-1B8F-48AD-8BC4-DDFD2EC9A00B".GuidStringToUInt128();

    public required string Name { get; init; }
    public required UInt128 EntityTypeId { get; init; }
    public required ValueTypes ValueType { get; init; }

    public required ulong Id { get; init; }
}
