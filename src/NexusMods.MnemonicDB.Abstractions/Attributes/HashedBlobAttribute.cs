using System;
using System.Buffers;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

public abstract class HashedBlobAttribute<TValue>(string ns, string name) : ScalarAttribute<TValue, byte[]>(ValueTags.HashedBlob, ns, name)
{
    public override void Write<TWriter>(EntityId entityId, RegistryId registryId, TValue value, TxId txId, bool isRetract, TWriter writer)
    {
        using var innerWriter = new PooledMemoryBufferWriter();
        WritePrefix(entityId, registryId, txId, isRetract, writer);

        WriteValue(value, innerWriter);
        var valueSpan = innerWriter.GetWrittenSpan();

        var hash = XxHash3.HashToUInt64(valueSpan);

        var writerSpan = writer.GetSpan(sizeof(ulong));
        MemoryMarshal.Write(writerSpan, hash);
        writer.Advance(sizeof(ulong));
        writer.Write(valueSpan);
    }


    /// <inheritdoc />
    protected override byte[] ToLowLevel(TValue value)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Overwrite this method to read the value from the reader
    /// </summary>
    protected abstract override TValue FromLowLevel(ReadOnlySpan<byte> value, ValueTags tag);

    /// <summary>
    /// Overwrite this method to write the value to the writer
    /// </summary>
    protected abstract void WriteValue<TWriter>(TValue value, TWriter writer)
        where TWriter : IBufferWriter<byte>;
}
