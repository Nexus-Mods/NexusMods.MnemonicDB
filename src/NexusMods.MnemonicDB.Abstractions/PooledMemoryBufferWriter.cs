using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     A IBufferWriter that uses pooled memory to reduce allocations.
/// </summary>
public sealed class PooledMemoryBufferWriter : IBufferWriter<byte>, IDisposable
{
    private readonly IMemoryOwner<byte> _owner;
    private Memory<byte> _data;
    private int _size;

    /// <summary>
    ///     Constructs a new pooled memory buffer writer, with the given initial capacity.
    /// </summary>
    /// <param name="initialCapacity"></param>
    public PooledMemoryBufferWriter(int initialCapacity = 1024)
    {
        _owner = MemoryPool<byte>.Shared.Rent(initialCapacity);
        _data = _owner.Memory;
        Length = 0;
        _size = initialCapacity;
    }

    /// <summary>
    /// Write the unmanaged structure to the buffer.
    /// </summary>
    public void Write<T>(in T structure) where T : unmanaged
    {
        var structureSpan = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in structure), 1);
        var byteSpan = MemoryMarshal.Cast<T, byte>(structureSpan);
        var outSpan = GetSpan(byteSpan.Length);
        byteSpan.CopyTo(outSpan);
        Advance(byteSpan.Length);
    }

    /// <summary>
    /// Creates a marker for this location in memory, allowing the structure to be written later via the returned
    /// marker.
    /// </summary>
    public void Mark<T>() where T : unmanaged
    {
        var d = default(T);
        var structureSpan = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in d), 1);
        var byteSpan = MemoryMarshal.Cast<T, byte>(structureSpan);
        var outSpan = GetSpan(byteSpan.Length);
        byteSpan.CopyTo(outSpan);
        Advance(byteSpan.Length);
    }

    /// <summary>
    /// A marker for a location in memory, allowing the structure to be written later.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="offset"></param>
    /// <typeparam name="T"></typeparam>
    public struct Marker<T>(PooledMemoryBufferWriter writer, int offset) where T : unmanaged
    {
        /// <summary>
        /// Write the structure to the buffer at the marker location.
        /// </summary>
        public void Set(T structure)
        {
            var structureSpan = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in structure), 1);
            var byteSpan = MemoryMarshal.Cast<T, byte>(structureSpan);
            byteSpan.CopyTo(writer.WrittenSpanWritable.SliceFast(offset, byteSpan.Length));
        }
    }


    /// <summary>
    /// Gets the written memory of this writer.
    /// </summary>
    public ReadOnlyMemory<byte> WrittenMemory => _data.Slice(0, Length);

    /// <summary>
    /// Gets the written span of this writer, as a writable span.
    /// </summary>
    public Span<byte> WrittenSpanWritable => _data.Span.SliceFast(0, Length);

    /// <summary>
    ///     Gets the written length of the data
    /// </summary>
    public int Length { get; private set; }
    
    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        if (Length + count >= _size)
            Expand(Length + count);
        Debug.Assert(Length + count <= _size);
        Length += count;
    }

    /// <inheritdoc />
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (Length + sizeHint > _size)
            Expand(Length + sizeHint);

        Debug.Assert(Length + sizeHint <= _size);
        return _data[Length..];
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (sizeHint == 0)
        {
            if (Length >= _size)
                Expand(Length + 1);
            return _data.Span.SliceFast(Length);
        }

        if (Length + sizeHint >= _size)
            Expand(Length + sizeHint);

        Debug.Assert(Length + sizeHint <= _size);
        return _data.Span.SliceFast(Length, sizeHint);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _owner.Dispose();
    }

    /// <summary>
    ///     Resets the buffer writer, allowing it to be reused.
    /// </summary>
    public void Reset()
    {
        Length = 0;
    }

    /// <summary>
    ///     Writes the given span to the buffer, and advances the length.
    /// </summary>
    public void Write(ReadOnlySpan<byte> span)
    {
        span.CopyTo(GetSpan(span.Length));
        Advance(span.Length);
    }

    /// <summary>
    /// Writes the given datom to the buffer.
    /// </summary>
    /// <param name="datom"></param>
    public void Write(in Datom datom)
    {
        var span = GetSpan(KeyPrefix.Size + datom.ValueSpan.Length);
        MemoryMarshal.Write(span, datom.Prefix);
        datom.ValueSpan.CopyTo(span.SliceFast(KeyPrefix.Size));
        Advance(KeyPrefix.Size + datom.ValueSpan.Length);
    }

    /// <summary>
    /// Writes the value to the buffer as-is, via MemoryMarshal.Write.
    /// </summary>
    public unsafe void WriteMarshal<T>(T value) where T : unmanaged
    {
        var span = GetSpan(sizeof(T));
        MemoryMarshal.Write(span, value);
        Advance(sizeof(T));
    }

    /// <summary>
    ///     Gets the written span.
    /// </summary>
    /// <returns></returns>
    public ReadOnlySpan<byte> GetWrittenSpan()
    {
        return _data.Span.SliceFast(0, Length);
    }

    /// <summary>
    /// Assumes the data written to this writer is a datom, converts the contents to a Datom.
    /// Note: this datom will share memory with this buffer, resetting the buffer will alter
    /// the contents of the datom. 
    /// </summary>
    public Datom AsDatom()
    {
        var prefix = MemoryMarshal.Read<KeyPrefix>(_data.Span);
        var valueSpan = _data.Slice(KeyPrefix.Size, Length - KeyPrefix.Size);
        return new Datom(prefix, valueSpan);
    }


    /// <summary>
    ///     Gets the written span, but allows it to be written to.
    /// </summary>
    public Span<byte> GetWrittenSpanWritable()
    {
        return _data.Span.SliceFast(0, Length);
    }

    private void Expand(int atLeast)
    {
        var newSize = _size;
        while (newSize < atLeast)
            newSize *= 2;

        var newData = new Memory<byte>(new byte[newSize]);
        _data.CopyTo(newData);
        _data = newData;
        _size = newSize;
    }
}
