using System;
using System.Buffers;
using NexusMods.EventSourcing.Storage.Interfaces;

namespace NexusMods.EventSourcing.Storage.Datoms;

public record OnHeapDatom() : IRawDatom
{
    public required ulong EntityId { get; init; }
    public required ushort AttributeId { get; init; }
    public required ulong TxId { get; init; }
    public required byte Flags { get; init; }
    public required ulong ValueLiteral { get; init; }
    public required byte[] ValueData { get; init; }

    public ReadOnlySpan<byte> ValueSpan => ValueData;

}
