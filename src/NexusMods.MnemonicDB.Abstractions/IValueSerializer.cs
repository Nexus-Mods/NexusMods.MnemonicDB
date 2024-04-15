using System;
using System.Buffers;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     An abstract interface for a value serializer. Serializers can either emit fixed
///     or variable sized segments.
/// </summary>
public interface IValueSerializer
{
    /// <summary>
    ///     The low level (DB) type for this serializer, such as UInt32, or Utf8String
    /// </summary>
    public ValueTags LowLevelType { get; }

    /// <summary>
    ///     The high level (C#) type for this serializer, such as Hash, Path, or Color
    /// </summary>
    public Type HighLevelType { get; }

    /// <summary>
    ///     The unique identifier for this serializer
    /// </summary>
    public Symbol UniqueId { get; }

}

/// <summary>
///     Fixed size serializers can also do range lookups and so must implement a comparitor
/// </summary>
public interface IValueSerializer<THighLevel, TLowLevel> : IValueSerializer
{
    /// <summary>
    ///     Reads from the Buffer returning the number of bytes consumed
    /// </summary>
    public THighLevel ToLowLevel(TLowLevel lowLevel);


    /// <summary>
    /// Converts a high level representation to a low level representation
    /// </summary>
    public TLowLevel ToHighLevel(THighLevel highLevel);
}
