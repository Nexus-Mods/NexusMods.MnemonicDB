using System.Collections.Frozen;
using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

[Entity("C65C698C-B995-455F-AA84-30DDE98F7C9D")]
public class Loadout : AEntity
{
    public Loadout(IDb context, EntityId id) : base(context, id) {
    }
    public Loadout(Transaction tx) : base(tx) {
    }

    public required string Name { get; init; }
}
