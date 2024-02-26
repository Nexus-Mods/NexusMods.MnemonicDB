using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public readonly struct Datom
{
    public EntityId E { get; init; }
    public AttributeId A { get; init; }
    public TxId T { get; init; }
    public DatomFlags F { get; init; }
    public ReadOnlyMemory<byte> V { get; init; }
}
