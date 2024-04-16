namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

public enum ValueTags : byte
{
    /// <summary>
    /// Null value, no data
    /// </summary>
    Null = 0,
    /// <summary>
    /// Unsigned 8-bit integer
    /// </summary>
    UInt8 = 1,
    /// <summary>
    /// Unsigned 16-bit integer
    /// </summary>
    UInt16 = 2,
    /// <summary>
    /// Unsigned 32-bit integer
    /// </summary>
    UInt32 = 3,
    /// <summary>
    /// Unsigned 64-bit integer
    /// </summary>
    UInt64 = 4,
    /// <summary>
    /// Unsigned 128-bit integer
    /// </summary>
    UInt128 = 5,
    /// <summary>
    /// Unsigned 16-bit integer
    /// </summary>
    Int16 = 6,
    /// <summary>
    /// Unsigned 32-bit integer
    /// </summary>
    Int32 = 7,
    /// <summary>
    /// Unsigned 64-bit integer
    /// </summary>
    Int64 = 8,
    /// <summary>
    /// Unsigned 128-bit integer
    /// </summary>
    Int128 = 9,
    /// <summary>
    /// 32-bit floating point number
    /// </summary>
    Float32 = 10,
    /// <summary>
    /// 64-bit floating point number (double)
    /// </summary>
    Float64 = 11,
    /// <summary>
    /// ASCII string, case-sensitive
    /// </summary>
    Ascii = 12,
    /// <summary>
    /// UTF-8 string, case-sensitive
    /// </summary>
    Utf8 = 13,
    /// <summary>
    /// UTF-8 string, case-insensitive
    /// </summary>
    Utf8Insensitive = 14,
    /// <summary>
    /// Inline binary data
    /// </summary>
    Blob = 15,

    /// <summary>
    /// A blob sorted by its xxHash64 hash, and where the data is possibly stored in a separate location
    /// as to degrade the performance of the key storage
    /// </summary>
    HashedBlob = 16,

    /// <summary>
    /// A reference to another entity
    /// </summary>
    Reference = 17,

}
