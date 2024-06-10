using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Assists in building keys for the datastore, packing multiple values into a single memory buffer.
/// </summary>
public class KeyBuilder
{
    private readonly RegistryId _registryId;

    /// <summary>
    /// Primary constructor, requires a registry id for resolving attribute ids.
    /// </summary>
    public KeyBuilder(RegistryId registryId)
    {
        _registryId = registryId;
    }

    /// <summary>
    /// Write a lower bound key for the given entity id, with other values set to their minimum.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public Memory<byte> From(EntityId e)
    {
        var writer = new PooledMemoryBufferWriter(32);
        var prefix = new KeyPrefix().Set(e, AttributeId.Min, TxId.MinValue, false);
        writer.WriteMarshal(prefix);
        writer.WriteMarshal((byte)ValueTags.Null);
        var output = GC.AllocateUninitializedArray<byte>(writer.Length);
        writer.WrittenMemory.Span.CopyTo(output);
        return output;
    }

    /// <summary>
    /// Write an upper bound key for the given entity id, with other values set to their maximum.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public Memory<byte> To(EntityId e)
    {
        var writer = new PooledMemoryBufferWriter(32);
        var prefix = new KeyPrefix().Set(e, AttributeId.Max, TxId.MaxValue, false);
        writer.WriteMarshal(prefix);
        writer.WriteMarshal((byte)ValueTags.Null);
        var output = GC.AllocateUninitializedArray<byte>(writer.Length);
        writer.WrittenMemory.Span.CopyTo(output);
        return output;
    }

}
