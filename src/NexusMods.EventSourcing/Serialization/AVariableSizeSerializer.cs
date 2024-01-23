using System;
using System.Buffers;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// A serializer that can handle variable sized types, and supports efficient encoding of lengths.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class AVariableSizeSerializer<T> : IVariableSizeSerializer<T>
{
    /// <inheritdoc />
    public virtual bool CanSerialize(Type valueType)
    {
        return valueType == typeof(T);
    }

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = 0;
        return false;
    }

    /// <inheritdoc />
    public abstract void Serialize<TWriter>(T value, TWriter output) where TWriter : IBufferWriter<byte>;

    /// <inheritdoc />
    public abstract int Deserialize(ReadOnlySpan<byte> from, out T value);

    /// <summary>
    /// Writes a length in a variable size int, this assumes that most lengths will be small, and so will be encoded in 1 byte
    /// unless they are larger than 255, in which case they will be encoded in 3 bytes with a one byte header. This means the maximum
    /// collection size is 16,777,215 items.
    /// </summary>
    /// <param name="output"></param>
    /// <param name="length"></param>
    /// <typeparam name="TWriter"></typeparam>
    protected void WriteLength<TWriter>(TWriter output, int length) where TWriter : IBufferWriter<byte>
    {
        if (length < byte.MaxValue)
        {
            var space = output.GetSpan(1);
            space[0] = (byte)length;
            output.Advance(1);
        }
        else
        {
            var space = output.GetSpan(4);
            // Set the length
            BinaryPrimitives.WriteUInt32BigEndian(space, (uint)length);
            // Overwrite the first byte with the header
            space[0] = byte.MaxValue;
            output.Advance(4);
        }
    }

    /// <summary>
    /// Reads a length in a variable size int, this assumes that most lengths will be small, and so will be encoded in 1 byte
    /// unless they are larger than 255, in which case they will be encoded in 3 bytes with a one byte header. This means the maximum
    /// collection size is 16,777,215 items.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    protected int ReadLength(ReadOnlySpan<byte> from, out int length)
    {
        if (from[0] == byte.MaxValue)
        {
            length = (int)BinaryPrimitives.ReadUInt32BigEndian(from) & 0x00FFFFFF;
            return 4;
        }
        length = from[0];
        return 1;
    }
}
