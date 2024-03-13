using System;
using System.Runtime.InteropServices;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A untyped tuple of (E, A, T, V) values.
/// </summary>
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
    /// Value Data
    /// </summary>
    public ReadOnlyMemory<byte> V { get; init; }

    /// <summary>
    /// A datom with the maximum possible values for each field.
    /// </summary>
    public static Datom Max = new()
    {
        E = EntityId.From(ulong.MaxValue),
        A = AttributeId.From(ulong.MaxValue),
        T = TxId.MaxValue,
        V = ReadOnlyMemory<byte>.Empty
    };

    /// <inheritdoc />
    public override string ToString()
    {
        return $"({E}, {A}, {T}, {Convert.ToHexString(V.Span)}))";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Datom other)
            return false;

        return E == other.E && A == other.A && T == other.T && V.Span.SequenceEqual(other.V.Span);
    }
}
