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
    /// The returned type of the value
    /// </summary>
    public Type ValueType { get; }
    
    /// <summary>
    /// Extract the segment part from the source datom segment and write it to the destination buffer.
    /// </summary>
    public void Extract(ReadOnlySpan<byte> src, Span<byte> dst, PooledMemoryBufferWriter writer);
}

/// <summary>
/// A typed column of a specific type
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IColumn<T> : IColumn 
    where T : unmanaged
{
    public unsafe int FixedSize => sizeof(T);
    
    public Type ValueType => typeof(T);
}
