using System;
using System.Buffers;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An abstract class for a blob attribute, the values for this attribute are inlined into the key in the datom store,
/// so try to keep the sizes small. The actual limit is based on the underlying storage engine. For larger values
/// use the HashedBlobAttribute.
/// </summary>
public abstract class BlobAttribute<TValue>(string ns, string name) : ScalarAttribute<TValue, byte[]>(ValueTags.Blob, ns, name)
{
    /// <inheritdoc />
    public override void Write<TWriter>(EntityId entityId, RegistryId registryId, TValue value, TxId txId, bool isRetract, TWriter writer)
    {
        WritePrefix(entityId, registryId, txId, isRetract, writer);
        WriteValue(value, writer);
    }

    protected override byte[] ToLowLevel(TValue value)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Overwrite this method to read the value from the reader
    /// </summary>
    protected abstract override TValue FromLowLevel(ReadOnlySpan<byte> value, ValueTags tags, RegistryId registryId);

    /// <summary>
    /// Overwrite this method to write the value to the writer
    /// </summary>
    protected abstract void WriteValue<TWriter>(TValue value, TWriter writer)
        where TWriter : IBufferWriter<byte>;
}
