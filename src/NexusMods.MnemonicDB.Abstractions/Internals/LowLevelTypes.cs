namespace NexusMods.MnemonicDB.Abstractions.Internals;

/// <summary>
/// The low level types that can be stored in the datom store
/// </summary>
public enum LowLevelTypes : byte
{
    /// <summary>
    /// Used mostly for index ranges that don't have a type
    /// </summary>
    Null = 0,

    /// <summary>
    /// A unsigned integer
    /// </summary>
    UInt,
    /// <summary>
    /// A signed integer
    /// </summary>
    SInt,

    /// <summary>
    /// A floating point number
    /// </summary>
    Float,

    /// <summary>
    /// UTF8 encoded string, compared case sensitively
    /// </summary>
    Utf8,

    /// <summary>
    /// UTF8 encoded string, compared case insensitively
    /// </summary>
    InsensitiveUtf8,

    /// <summary>
    /// A reference to another entity
    /// </summary>
    Reference,

    /// <summary>
    /// Raw ASCII bytes, not UTF8 encoded
    /// </summary>
    Ascii,

    /// <summary>
    /// A binary blob
    /// </summary>
    Blob = 15,

}
