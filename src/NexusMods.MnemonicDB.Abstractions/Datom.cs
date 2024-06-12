using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.DatomIterators;

/// <summary>
/// Represents a raw (unparsed) datom from an index. Most of the time this datom is only valid for the
/// lifetime of the current iteration. It is not safe to store this datom for later use.
/// </summary>
public struct Datom(ReadOnlyMemory<byte> memory, IAttributeRegistry registry)
{
    /// <summary>
    /// A span of the raw datom
    /// </summary>
    public ReadOnlySpan<byte> RawSpan => memory.Span;

    /// <summary>
    /// The KeyPrefix of the datom
    /// </summary>
    public KeyPrefix Prefix => MemoryMarshal.Read<KeyPrefix>(RawSpan);

    /// <summary>
    /// The valuespan of the datom
    /// </summary>
    public ReadOnlySpan<byte> ValueSpan => RawSpan.SliceFast(KeyPrefix.Size);

    /// <summary>
    /// The attribute registry for this datom
    /// </summary>
    public IAttributeRegistry Registry => registry;

    /// <summary>
    /// Resolves this datom into the IReadDatom form
    /// </summary>
    public IReadDatom Resolved => registry.Resolve(RawSpan);

    /// <summary>
    /// Resolves the value of the datom into the given type
    /// </summary>
    public TValue Resolve<TValue, TLowLevel>(Attribute<TValue, TLowLevel> attribute) =>
        attribute.ReadValue(ValueSpan);

    /// <summary>
    /// EntityId of the datom
    /// </summary>
    public EntityId E => Prefix.E;

    /// <summary>
    /// AttributeId of the datom
    /// </summary>
    public AttributeId A => Prefix.A;

    /// <summary>
    /// TxId of the datom
    /// </summary>
    public TxId T => Prefix.T;

    /// <summary>
    /// True if the datom is a retract
    /// </summary>
    public bool IsRetract => Prefix.IsRetract;

    /// <summary>
    /// Copies the data of this datom onto the heap so it's detached from the current iteration.
    /// </summary>
    public Datom Clone()
    {
        var copy = new byte[memory.Length];
        memory.Span.CopyTo(copy);
        return new Datom(copy, registry);
    }

    /// <summary>
    /// Returns true if the datom is valid
    /// </summary>
    public bool Valid => memory.Length > 0;

    /// <inheritdoc />
    public override string ToString()
    {
        return Resolved.ToString()!;
    }

    /// <summary>
    /// Returns -1 if this datom is less than the other, 0 if they are equal, and 1 if this datom is greater than the other.
    /// in relation to the given index type.
    /// </summary>
    public int Compare(Datom other, IndexType indexType)
    {
        switch (indexType)
        {
            case IndexType.TxLog:
                return DatomComparators.TxLogComparator.Compare(RawSpan, other.RawSpan);
            case IndexType.EAVTCurrent:
            case IndexType.EAVTHistory:
                return DatomComparators.EAVTComparator.Compare(RawSpan, other.RawSpan);
            case IndexType.AEVTCurrent:
            case IndexType.AEVTHistory:
                return DatomComparators.AEVTComparator.Compare(RawSpan, other.RawSpan);
            case IndexType.AVETCurrent:
            case IndexType.AVETHistory:
                return DatomComparators.AVETComparator.Compare(RawSpan, other.RawSpan);
            case IndexType.VAETCurrent:
            case IndexType.VAETHistory:
                return DatomComparators.VAETComparator.Compare(RawSpan, other.RawSpan);
            default:
                throw new ArgumentOutOfRangeException(nameof(indexType), indexType, "Unknown index type");
        }
    }

    /// <summary>
    /// Clone this datom and return it as a retraction datom
    /// </summary>
    /// <returns></returns>
    public Datom Retract()
    {
        var data = GC.AllocateUninitializedArray<byte>(RawSpan.Length);
        var dataSpan = data.AsSpan();
        RawSpan.CopyTo(dataSpan);
        var prefix = MemoryMarshal.Read<KeyPrefix>(dataSpan);
        var newPrefix = new KeyPrefix().Set(prefix.E, prefix.A, TxId.Tmp, true);
        MemoryMarshal.Write(dataSpan, newPrefix);
        return new Datom(data, registry);
    }
}
