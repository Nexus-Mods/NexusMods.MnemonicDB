using DuckDB.NET.Native;

namespace NexusMods.MnemonicDB.QueryV2;

/// <summary>
/// Generic wrapper for a vector of values.
/// </summary>
public interface IVector
{
    
    /// <summary>
    /// Returns true if the element at the given index is valid if false, it should be considered as a null value.
    /// </summary>
    bool IsValid(int index);
}

/// <summary>
/// A vector of a specific low-level type and serializer for that type.
/// </summary>
public interface ILowLevelVector<out TLowLevel> : IVector
{
    /// <summary>
    /// Get the low-level value at the given index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public TLowLevel GetLowLevel(int index);
}


/// <summary>
/// An interface specifically for high-level vectors, which only exposes the high-level type.
/// </summary>
public interface IHighLevelVector<out THighLevel, TThis> : IVector 
    where TThis : IHighLevelVector<THighLevel, TThis>
{
    public THighLevel this[int index] { get; }
    
    
    public void Reset(DuckDBDataChunk chunk, int column);

    public static abstract TThis Create(DuckDBDataChunk chunk, int column);
}
