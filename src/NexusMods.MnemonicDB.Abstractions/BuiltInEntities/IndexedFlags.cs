using System;

namespace NexusMods.MnemonicDB.Abstractions.BuiltInEntities;

/// <summary>
/// The indexed flags for an attribute
/// </summary>
[Flags]
public enum IndexedFlags : byte
{
    /// <summary>
    /// Attribute is not indexed
    /// </summary>
    None = 0b0000,
    /// <summary>
    /// The attribute values are indexed
    /// </summary>
    Indexed = 0b0001,
    
    /// <summary>
    /// The attribute values have a unique index
    /// </summary>
    Unique = 0b0011,
}
