using System;
using System.Buffers.Binary;
using System.Globalization;

namespace NexusMods.EventSourcing.Abstractions;


public readonly struct EntityId : IEquatable<EntityId>, IComparable<EntityId>
{
    /// <summary>
    /// Creates a new instance of <see cref="EntityId"/>.
    /// </summary>
    /// <param name="id"></param>
    public EntityId(UInt128 id) => Value = id;

    /// <summary>
    /// The Id as a UInt128.
    /// </summary>
    public readonly UInt128 Value;

    /// <summary>
    /// Constructs a new EntityId from the given UInt128.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static EntityId From(UInt128 id) => new(id);

    /// <summary>
    /// Reads the <see cref="EntityId"/> from the specified <paramref name="span"/>.
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public static EntityId From(ReadOnlySpan<byte> span)
    {
        return From(BinaryPrimitives.ReadUInt128BigEndian(span));
    }

    /// <summary>
    /// Writes the <see cref="EntityId"/> to the specified <paramref name="span"/>.
    /// </summary>
    /// <param name="span"></param>
    public void WriteTo(Span<byte> span)
    {
        BinaryPrimitives.WriteUInt128BigEndian(span, Value);
    }

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
    /// Compares two <see cref="EntityId"/>s for equality.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(EntityId other)
    {
        return Value.Equals(other.Value);
    }

    /// <summary>
    /// Compares two <see cref="EntityId"/>s for equality.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(EntityId left, EntityId right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="EntityId"/>s for inequality.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(EntityId left, EntityId right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Compares two <see cref="EntityId"/>s for equality.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        return obj is EntityId other && Equals(other);
    }

    /// <summary>
    /// Gets the hash code for the <see cref="EntityId"/>.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// Compares two <see cref="EntityId"/>s.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(EntityId other)
    {
        return Value.CompareTo(other.Value);
    }
}

/// <summary>
/// A typed <see cref="EntityId"/> for a specific <see cref="IEntity"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct EntityId<T> : IEquatable<EntityId<T>>, IComparable<EntityId<T>> where T : IEntity
{
    /// <summary>
    /// Creates a new instance of <see cref="EntityId{T}"/>.
    /// </summary>
    /// <param name="id"></param>
    public EntityId(EntityId id) => Id = id;

    /// <summary>
    /// The underlying id value
    /// </summary>
    public readonly EntityId Id;

    /// <summary>
    /// Creates a new instance of <see cref="EntityId{T}"/>.
    /// </summary>
    /// <returns></returns>
    public static EntityId<T> NewId() => new(EntityId.NewId());

    /// <summary>
    /// Converts the <see cref="EntityId{T}"/> to a <see cref="EntityId"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static EntityId<T> From(EntityId id) => new(id);

    /// <summary>
    /// Converts the <see cref="EntityId{T}"/> to a <see cref="EntityId"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static EntityId<T> From(UInt128 id) => new(EntityId.From(id));

    /// <summary>
    /// Creates a new instance of <see cref="EntityId{T}"/> from the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public static EntityId<T> From(ReadOnlySpan<byte> span)
    {
        return From(BinaryPrimitives.ReadUInt128BigEndian(span));
    }

    /// <summary>
    /// Converts the <see cref="EntityId{T}"/> to a <see cref="EntityId{T}"/> of another type. This is a hard, explicit
    /// cast, so it will not throw an exception if the cast is invalid.
    /// </summary>
    /// <typeparam name="TTo"></typeparam>
    /// <returns></returns>
    public EntityId<TTo> Cast<TTo>() where TTo : IEntity => new(Id);

    /// <summary>
    /// Gets the <see cref="EntityId{T}"/> from the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static EntityId<T> From(ReadOnlySpan<char> id)
    {
        if (Guid.TryParse(id, out var guid))
        {
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);
            return From(BinaryPrimitives.ReadUInt128BigEndian(bytes));
        }
        return From(UInt128.Parse(id, NumberStyles.HexNumber));
    }

    /// <inheritdoc />
    public bool Equals(EntityId<T> other)
    {
        return Id.Equals(other.Id);
    }

    /// <inheritdoc />
    public int CompareTo(EntityId<T> other)
    {
        return Id.CompareTo(other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is EntityId<T> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Compares two <see cref="EntityId{T}"/>s for equality.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(EntityId<T> left, EntityId<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="EntityId{T}"/>s for inequality.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(EntityId<T> left, EntityId<T> right)
    {
        return !(left == right);
    }
}
