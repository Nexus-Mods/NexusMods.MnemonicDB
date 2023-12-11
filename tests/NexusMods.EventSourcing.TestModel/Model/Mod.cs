using NexusMods.EventSourcing.Abstractions;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.EventSourcing.TestModel.Model;

public class Mod : IEntity
{
    public EntityId Id { get; internal set; }

    [Reactive]
    public string Name { get; internal set; } = string.Empty;

    [Reactive]
    public bool Enabled { get; internal set; }

    [Reactive]
    public Collection? Collection { get; internal set; }
}
