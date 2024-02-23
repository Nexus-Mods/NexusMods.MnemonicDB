using System;
using System.Buffers.Binary;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Helper extensions for the event sourcing library
/// </summary>
public static class HelperExtensions
{
    /// <summary>
    /// Assumes the string is a valid GUID and converts it to a UInt128
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static UInt128 ToUInt128Guid(this string guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        if (!Guid.TryParse(guid, out var parsedGuid))
            throw new ArgumentException("Invalid GUID", nameof(guid));
        parsedGuid.TryWriteBytes(bytes);
        return BinaryPrimitives.ReadUInt128LittleEndian(bytes);
    }

    /// <summary>
    /// Converts a GUID to a UInt128
    /// </summary>
    public static UInt128 ToUInt128Guid(this Guid guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);
        return BinaryPrimitives.ReadUInt128LittleEndian(bytes);
    }

}
