namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// All the value types that can be returned from a result set
/// </summary>
public enum ValueTypes : int
{
    /// <summary>
    /// Internal value, should not be returned
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Int64 value
    /// </summary>
    Int64 = 1,
    /// <summary>
    /// UInt64 value
    /// </summary>
    UInt64 = 2,
    /// <summary>
    /// String value
    /// </summary>
    String = 3,
    /// <summary>
    /// Boolean value
    /// </summary>
    UHugeInt = 4,
    /// <summary>
    /// Double value
    /// </summary>
    Double = 5,
    /// <summary>
    /// Float value
    /// </summary>
    Float = 6,
    /// <summary>
    /// Byte blob value
    /// </summary>
    Bytes = 7
}
