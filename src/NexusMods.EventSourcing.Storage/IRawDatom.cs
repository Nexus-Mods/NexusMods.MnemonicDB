using System;
using System.Buffers;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage;

public interface IRawDatom
{
    public ulong EntityId{ get; }

    public ushort AttributeId { get; }

    public ulong TxId { get; }

    public byte Flags { get; }

    public ReadOnlySpan<byte> ValueSpan { get; }

    public ulong ValueLiteral { get; }

}

public static class RawDatomExtensions
{
    public static bool EquivelentTo<TDatomA, TDatomB>(this TDatomA a, in TDatomB b)
        where TDatomA : IRawDatom
        where TDatomB : IRawDatom
    {
        return a.EntityId == b.EntityId &&
               a.AttributeId == b.AttributeId &&
               a.TxId == b.TxId &&
               a.Flags == b.Flags &&
               a.ValueLiteral == b.ValueLiteral &&
               a.ValueSpanEquals(b);
    }

    private static bool ValueSpanEquals<TDatomA, TDatomB>(this TDatomA a, in TDatomB b)
        where TDatomA : IRawDatom
        where TDatomB : IRawDatom
    {
        if (!((DatomFlags)a.Flags).HasFlag(DatomFlags.InlinedData))
        {
            return a.ValueSpan.SequenceEqual(b.ValueSpan);
        }

        return true;
    }
}
