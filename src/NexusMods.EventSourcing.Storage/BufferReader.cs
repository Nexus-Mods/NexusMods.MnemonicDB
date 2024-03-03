using System;
using System.Runtime.InteropServices;

namespace NexusMods.EventSourcing.Storage;

public class BufferReader
{
    private readonly ReadOnlyMemory<byte> _memory;
    private int _idx;

    public BufferReader(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
        _idx = 0;
    }

    public T Read<T>() where T : unmanaged
    {
        var size = Marshal.SizeOf<T>();
        var span = _memory.Span.Slice(_idx, size);
        _idx += size;
        return MemoryMarshal.Read<T>(span);
    }

    public ReadOnlyMemory<byte> ReadMemory(int length)
    {
        var slice = _memory.Slice(_idx, length);
        _idx += length;
        return slice;
    }

    /// <summary>
    /// Read a FourCC from the buffer.
    /// </summary>
    /// <returns></returns>
    public FourCC ReadFourCC()
    {
        var span = _memory.Span.Slice(_idx, 4);
        _idx += 4;
        return FourCC.From(span);
    }
}
