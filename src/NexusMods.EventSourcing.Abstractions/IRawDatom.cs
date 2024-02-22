using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents a datom with no interpretation of the values. Used to inject
/// structs into various functions to allow for comparison and serialization.
/// </summary>
public interface IRawDatom
{
    /// <summary>
    /// Entity id of the datom.
    /// </summary>
    public ulong EntityId{ get; }

    /// <summary>
    /// Attribute id of the datom.
    /// </summary>
    public ushort AttributeId { get; }

    /// <summary>
    /// Transaction id of the datom.
    /// </summary>
    public ulong TxId { get; }

    /// <summary>
    /// Flags of the datom.
    /// </summary>
    public DatomFlags Flags { get; }

    /// <summary>
    /// Valuespan of the datom, may be empty
    /// </summary>
    public ReadOnlySpan<byte> ValueSpan { get; }

    /// <summary>
    /// Value of the datom, if it is inlined, otherwise it's 32 bits for both the offset and the length.
    /// </summary>
    public ulong ValueLiteral { get; }

}

/// <summary>
/// Extension methods for <see cref="IRawDatom"/>.
/// </summary>
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

    /// <summary>
    /// Common toString method for IRawDatom
    /// </summary>
    /// <param name="d"></param>
    /// <returns></returns>
    public static string CommonToString<TDatom>(this TDatom d)
    where TDatom : IRawDatom
    {
        if (d.Flags.HasFlag(DatomFlags.InlinedData))
            return $"({d.EntityId}, {d.AttributeId}, {d.TxId}, {(byte)d.Flags} {d.ValueLiteral})";
        var hex = Convert.ToHexString(d.ValueSpan);
        return $"({d.EntityId}, {d.AttributeId}, {d.TxId}, {(byte)d.Flags}, [{hex}])";
    }
}
