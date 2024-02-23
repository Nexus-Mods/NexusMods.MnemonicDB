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

    public override string ToString()
    {
        return this.CommonToString();
    }

    /// <summary>
    /// All the values of the datom set to their maximum value.
    /// </summary>
    public static readonly OnHeapDatom Max = new()
    {
        EntityId = ulong.MaxValue,
        AttributeId = ushort.MaxValue,
        ValueLiteral = ulong.MaxValue,
        ValueData = [],
        TxId = ulong.MaxValue,
        Flags = DatomFlags.Added | DatomFlags.InlinedData
    };

    public static OnHeapDatom Create<TDatom>(in TDatom a)
    where TDatom : IRawDatom
    {
        return new OnHeapDatom
        {
            EntityId = a.EntityId,
            AttributeId = a.AttributeId,
            ValueLiteral = a.ValueLiteral,
            ValueData = a.Flags.HasFlag(DatomFlags.InlinedData) ? [] : a.ValueSpan.ToArray(),
            TxId = a.TxId,
            Flags = a.Flags
        };
    }
}
