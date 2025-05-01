using System;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A column definition
/// </summary>
public interface IColumn
{
    /// <summary>
    /// The size in bytes of the fixed part of the column.
    /// </summary>
    public int FixedSize { get; }
    
    /// <summary>
    /// Extract the segment part from the source datom segment and write it to the destination buffer.
    /// </summary>
    public void Extract(ReadOnlySpan<byte> keySpan, Span<byte> dst, PooledMemoryBufferWriter writer);
}
