using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

public readonly struct ValueWriter
{
    /// <summary>
    /// Writes the ulong value to the output span
    /// </summary>
    public static void Write(ref KeyPrefix prefix, ulong value, Span<byte> output)
    {
        prefix.ValueLength = sizeof(ulong);
        prefix.LowLevelType = LowLevelTypes.UInt;
        MemoryMarshal.Write(output, value);
    }

    /// <summary>
    /// Writes an EntityId to the output span
    /// </summary>
    public static void Write(ref KeyPrefix prefix, EntityId value, Span<byte> output)
    {
        Write(ref prefix, value.Value, output);
    }
}
