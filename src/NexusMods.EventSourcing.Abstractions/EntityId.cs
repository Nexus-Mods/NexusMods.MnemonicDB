using System;
using System.Buffers.Binary;
using System.Globalization;
using TransparentValueObjects;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A unique identifier for an <see cref="IEntity"/>.
/// </summary>
[ValueObject<UInt128>]
public readonly partial struct EntityId
{
    /// <summary>
    /// Casts this <see cref="EntityId"/> to a <see cref="EntityId{T}"/> of the specified type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public EntityId<T> Cast<T>() where T : IEntity => new(this);

    /// <summary>
    /// Creates a random <see cref="EntityId"/>.
    /// </summary>
    /// <returns></returns>
    public static EntityId NewId()
    {
        var guid = Guid.NewGuid();
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);
        var value = BinaryPrimitives.ReadUInt128BigEndian(bytes);
        return From(value);
    }

    /// <summary>
    /// Reads the <see cref="EntityId"/> from the specified <paramref name="data"/>.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static EntityId From(ReadOnlySpan<byte> data) => new(BinaryPrimitives.ReadUInt128BigEndian(data));

    /// <summary>
    /// Writes the <see cref="EntityId"/> to the specified <paramref name="span"/>.
    /// </summary>
    /// <param name="span"></param>
    public void TryWriteBytes(Span<byte> span)
    {
        BinaryPrimitives.WriteUInt128BigEndian(span, Value);
    }
}


/// <summary>
/// A strongly typed <see cref="EntityId"/> for a specific <see cref="IEntity"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct EntityId<T> where T : IEntity
{
    /// <summary>
    /// Creates a new instance of <see cref="EntityId{T}"/>.
    /// </summary>
    /// <returns></returns>
    public static EntityId<T> NewId() => new(EntityId.NewId());


    /// <summary>
    /// Gets the <see cref="EntityId{T}"/> from the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static EntityId<T> From(UInt128 id) => new(EntityId.From(id));

    /// <summary>
    /// Reads the <see cref="EntityId{T}"/> from the specified <paramref name="data"/>.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static EntityId<T> From(ReadOnlySpan<byte> data) => new(EntityId.From(data));


    /// <summary>
    /// Converts the <see cref="EntityId{T}"/> to a <see cref="EntityId"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static implicit operator EntityId(EntityId<T> id) => id.Value;


    /// <summary>
    /// Gets the <see cref="EntityId{T}"/> from the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static EntityId<T> From(string id)
    {
        if (Guid.TryParse(id, out var guid))
        {
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);
            return From(BinaryPrimitives.ReadUInt128BigEndian(bytes));
        }
        return From(UInt128.Parse(id, NumberStyles.HexNumber));
    }


    /// <summary>
    /// Creates a new instance of <see cref="EntityId{T}"/>.
    /// </summary>
    /// <param name="id"></param>
    public EntityId(EntityId id) => Value = id;

    /// <summary>
    /// The underlying value.
    /// </summary>
    public readonly EntityId Value;

    /// <inheritdoc />
    public override string ToString()
    {
        return typeof(T).Name + "<" + Value.Value.ToString("X") + ">";
    }


    /// <summary>
    /// Converts the <see cref="EntityId{T}"/> to a <see cref="EntityId{T}"/> of another type.
    /// </summary>
    /// <typeparam name="TTo"></typeparam>
    /// <returns></returns>
    public EntityId<TTo> Cast<TTo>() where TTo : IEntity => new(Value);
}
