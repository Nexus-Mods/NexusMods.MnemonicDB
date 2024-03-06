using System;
using System.Runtime.InteropServices;

namespace NexusMods.EventSourcing.Abstractions;

public readonly struct Datom
{
    /// <summary>
    /// Entity id.
    /// </summary>
    public EntityId E { get; init; }

    /// <summary>
    /// Attribute id
    /// </summary>
    public AttributeId A { get; init; }

    /// <summary>
    /// TX id
    /// </summary>
    public TxId T { get; init; }

    /// <summary>
    /// Flags
    /// </summary>
    public DatomFlags F { get; init; }

    /// <summary>
    /// Value Data
    /// </summary>
    public ReadOnlyMemory<byte> V { get; init; }

    public static Datom Max = new()
    {
        E = EntityId.From(ulong.MaxValue),
        A = AttributeId.From(ulong.MaxValue),
        T = TxId.MaxValue,
        F = (DatomFlags)byte.MaxValue,
        V = ReadOnlyMemory<byte>.Empty
    };

    /// <summary>
    /// Assumes the value is a struct and unmarshals it.
    /// </summary>
    public T Unmarshal<T>() where T : struct
    {
        return MemoryMarshal.Read<T>(V.Span);
    }

    public override string ToString()
    {
        return $"({E}, {A}, {T}, {F}, {Convert.ToHexString(V.Span)}))";
    }
}
