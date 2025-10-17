using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

public interface ILowLevelIterator : IDisposable
{
    /// <summary>
    /// Move the iterator to the first datom that matches the given span.
    /// </summary>
    public void SeekTo(scoped ReadOnlySpan<byte> span);

    /// <summary>
    /// Move the iterator to the first datom that matches the given span (assumes a ValueTag of Null)
    /// </summary>
    public void SeekTo(in KeyPrefix prefix)
    {
        Debug.Assert(prefix.ValueTag == ValueTag.Null);
        var spanTo = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in prefix, 1));
        SeekTo(spanTo);
    }
}
