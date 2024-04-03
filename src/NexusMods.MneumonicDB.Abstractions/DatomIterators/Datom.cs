using System;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB.Abstractions.DatomIterators;

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
}
