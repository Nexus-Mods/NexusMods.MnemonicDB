using System.Collections.Frozen;
using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

public class Loadout(IDb context, EntityId id) : AEntity(context, id)
{
    public required string Name { get; init; }
}
