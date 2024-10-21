using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage.Abstractions;

/// <summary>
/// A combination of a IndexType and a Datom
/// </summary>
public readonly struct IndexDatom
{
    /// <summary>
    /// The Datom
    /// </summary>
    public Datom Datom { get; init; }
    
    /// <summary>
    /// The Index
    /// </summary>
    public IndexType Index { get; init; }

    public byte[] ToArray()
    {
        var size = KeyPrefix.Size + 1 + Datom.ValueSpan.Length;
        var data = GC.AllocateUninitializedArray<byte>(size);
        var span = data.AsSpan();
        ToSpan(span);
        return data;
    }

    private void ToSpan(Span<byte> span)
    {
        span[0] = (byte)Index;
        MemoryMarshal.Write(span.SliceFast(1), Datom.Prefix);
        Datom.ValueSpan.CopyTo(span.SliceFast(1 + KeyPrefix.Size));
    }

    /// <summary>
    /// Write this struct to the given writer
    /// </summary>
    public void Write<TWriter>(TWriter writer)
      where TWriter : IBufferWriter<byte>
    {
        var size = KeyPrefix.Size + 1 + Datom.ValueSpan.Length;
        var span = writer.GetSpan(size);
        ToSpan(span);
        writer.Advance(size);
    }
}
