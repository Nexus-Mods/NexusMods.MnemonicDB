using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage;

public static class BufferWriterExtensions
{
    /// <summary>
    /// Write the given integer to the buffer writer.
    /// </summary>
    public static void WriteFourCC<TWriter>(this TWriter writer, FourCC nodeType)
        where TWriter : IBufferWriter<byte>
    {
        nodeType.WriteTo(writer);
    }

    /// <summary>
    /// Write the given integer to the buffer writer.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="val"></param>
    /// <typeparam name="TWriter"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    public static void Write<TWriter, TVal>(this TWriter writer, TVal val)
    where TVal : unmanaged, IBinaryInteger<TVal>
    where TWriter : IBufferWriter<byte>
    {
        unsafe
        {
            Span<byte> span = stackalloc byte[sizeof(TVal)];
            MemoryMarshal.Write(span, val);
            writer.Write(span);
        }
    }
}
