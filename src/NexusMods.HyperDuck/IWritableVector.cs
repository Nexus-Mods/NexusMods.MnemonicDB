using System;

namespace NexusMods.HyperDuck;

public interface IWritableVector
{
    /// <summary>
    /// Gets the data of the vector as a span of T. Auto sizes the span to the number of elements supported by the vector
    /// </summary>
    Span<T> GetData<T>() where T : unmanaged;

    /// <summary>
    /// Get the writable validity mask for the vector
    /// </summary>
    public WritableValidityMask GetValidityMask();

    /// <summary>
    /// Write a UTF-8 span of bytes to the vector for the given row
    /// </summary>
    void WriteUtf8(ulong idx, ReadOnlySpan<byte> data);
    
    /// <summary>
    /// Write a UTF-16 string to the vector for the given row
    /// </summary>
    void WriteUtf16(ulong idx, string data);

    /// <summary>
    /// Return a writable vector for the nth element of the struct
    /// </summary>
    WritableVector GetStructChild(ulong idx);
    
    /// <summary>
    /// Gets a pointer to the underlying DuckDB vector
    /// </summary>
    internal unsafe void* GetPtr();
}

public static class WritableVectorExtensions
{
    
    /// <summary>
    /// Return a writable vector for the nth element of the struct
    /// </summary>
    public static WritableVector GetStructChild<TVector>(this TVector vector, int idx) 
        where TVector : IWritableVector
    {
        return vector.GetStructChild((ulong)idx);
    }
}
