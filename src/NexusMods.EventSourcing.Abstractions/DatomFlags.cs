using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Flags for datoms
/// </summary>
[Flags]
public enum DatomFlags : byte
{
    /// <summary>
    /// True if the datom is an addition, false if it is a retraction
    /// </summary>
    Added = 0x01,
    /// <summary>
    /// True if the datom is inlined in the 64-bit value, false if it is a reference to a value in the
    /// datom storage value blob
    /// </summary>
    InlinedData = 0x02,
}
