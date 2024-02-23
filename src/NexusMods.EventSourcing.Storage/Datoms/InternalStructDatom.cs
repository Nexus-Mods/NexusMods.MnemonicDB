using System;
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Datoms;

/// <summary>
/// A datom that's used internally with some sort of out-of-band blob store, designed
/// to be wrapped by something that can access the value span.
/// </summary>
public struct InternalStructDatom : IRawDatom
{
    public static InternalStructDatom Create<TOther>(in TOther other, ref PooledMemoryBufferWriter writer)
    where TOther : IRawDatom
    {
        if (other.Flags.HasFlag(DatomFlags.InlinedData))
            return new InternalStructDatom
            {
                EntityId = other.EntityId,
                AttributeId = other.AttributeId,
                TxId = other.TxId,
                Flags = other.Flags,
                ValueLiteral = other.ValueLiteral
            };

        var offset = writer.GetWrittenSpan().Length;
        var span = writer.GetSpan(other.ValueSpan.Length);
        other.ValueSpan.CopyTo(span);
        writer.Advance(other.ValueSpan.Length);
        return new InternalStructDatom
        {
            EntityId = other.EntityId,
            AttributeId = other.AttributeId,
            TxId = other.TxId,
            Flags = other.Flags | DatomFlags.InlinedData,
            ValueLiteral = (ulong)offset << 32 | (uint)other.ValueSpan.Length
        };
    }

    public ulong EntityId { get; init; }
    public ushort AttributeId { get; init; }
    public ulong TxId { get; init; }
    public DatomFlags Flags { get; init; }
    public ReadOnlySpan<byte> ValueSpan => throw new NotSupportedException();
    public ulong ValueLiteral { get; init; }
}
