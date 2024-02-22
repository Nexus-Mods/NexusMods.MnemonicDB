using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Datoms;

/// <summary>
/// A datom that is stored as a class, and exists on the heap.
/// </summary>
public class OnHeapDatom : IRawDatom
{
    public required ulong EntityId { get; init; }
    public required ushort AttributeId { get; init; }
    public required ulong TxId { get; init; }
    public required DatomFlags Flags { get; init; }
    public required ulong ValueLiteral { get; init; }
    public required byte[] ValueData { get; init; }

    public ReadOnlySpan<byte> ValueSpan => ValueData;

}
