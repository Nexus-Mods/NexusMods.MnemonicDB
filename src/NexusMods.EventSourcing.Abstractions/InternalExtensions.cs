using System;
using System.Buffers.Binary;

namespace NexusMods.EventSourcing.Abstractions;

static class InternalExtensions
{
    /// <summary>
    /// Converts a string representation of a GUID to a UInt128
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public static UInt128 GuidStringToUInt128(this string guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        Guid.TryParse(guid, out var guidValue);
        guidValue.TryWriteBytes(bytes);
        var id = BinaryPrimitives.ReadUInt64BigEndian(bytes);
        return id;
    }

}
