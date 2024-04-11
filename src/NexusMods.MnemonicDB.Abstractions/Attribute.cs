using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
/// <typeparam name="TValueType"></typeparam>
public sealed class Attribute<TValueType> : IAttribute
{
    private IValueSerializer<TValueType> _serializer = null!;
    private RegistryId.InlineCache _cache;

    public Attribute(string nsAndName, bool isIndexed = false, bool noHistory = false, Cardinality cardinality = Cardinality.One)
    {
        Id = Symbol.Intern(nsAndName);
        Cardinalty = cardinality;
        IsIndexed = isIndexed;
        NoHistory = noHistory;
    }

    /// <inheritdoc />
    public Symbol Id { get; }

    /// <inheritdoc />
    public Cardinality Cardinalty { get; }

    /// <inheritdoc />
    public bool IsIndexed { get; }

    /// <inheritdoc />
    public bool NoHistory { get; }

    /// <inheritdoc />
    public bool Equals(Attribute<TValueType>? other)
    {
        return other != null && ReferenceEquals(Id, other.Id);
    }

    IValueSerializer IAttribute.Serializer => Serializer;

    /// <inheritdoc />
    public void Add(ITransaction tx, EntityId entity, TValueType value)
    {
        tx.Add(entity, this, value);
    }

    /// <inheritdoc />
    public void SetDbId(RegistryId id, AttributeId attributeId)
    {
        _cache[id.Value] = attributeId;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AttributeId GetDbId(RegistryId id)
    {
        return _cache[id.Value];
    }

    /// <inheritdoc />
    public void SetSerializer(IValueSerializer serializer)
    {
        if (serializer is not IValueSerializer<TValueType> valueSerializer)
            throw new InvalidOperationException(
                $"Serializer {serializer.GetType()} is not compatible with {typeof(TValueType)}");
        _serializer = valueSerializer;
    }

    /// <inheritdoc />
    public Type ValueType => typeof(TValueType);

    /// <inheritdoc />
    public bool IsReference => typeof(TValueType) == typeof(EntityId);

    /// <inheritdoc />
    public IReadDatom Resolve(EntityId entityId, AttributeId attributeId, ReadOnlySpan<byte> value, TxId tx,
        bool isRetract)
    {
        return new ReadDatom
        {
            E = entityId,
            A = this,
            V = _serializer.Read(value),
            T = tx,
            IsRetract = isRetract
        };
    }

    /// <inheritdoc />
    public Type GetReadDatomType()
    {
        return typeof(ReadDatom);
    }

    /// <inheritdoc />
    public IValueSerializer<TValueType> Serializer => _serializer;


    /// <summary>
    /// Gets the value for this attribute on the given entity
    /// </summary>
    public TValueType Get(IEntity entity)
    {
        var segment = entity.Db.GetSegment(entity.Id);
        var dbId = _cache[segment.RegistryId.Value];
        for (var i = 0; i < segment.Count; i++)
        {
            var datom = segment[i];
            if (datom.A == dbId)
            {
                return datom.Resolve<TValueType>();
            }
        }
        ThrowKeyNotFoundException(entity.Id);
        return default!;
    }

    private void ThrowKeyNotFoundException(EntityId id)
    {
        throw new KeyNotFoundException($"Attribute {Id} not found on entity {id}");
    }

    /// <summary>
    /// Gets all values for this attribute on the given entity
    /// </summary>
    public Values<TValueType> GetAll(IEntity ent)
    {
        var segment = ent.Db.GetSegment(ent.Id);
        var dbId = _cache[segment.RegistryId.Value];
        for (var i = 0; i < segment.Count; i++)
        {
            var datom = segment[i];
            if (datom.A != dbId) continue;

            var start = i;
            while (i < segment.Count && segment[i].A == dbId)
            {
                i++;
            }
            return new Values<TValueType>(segment, start, i, Serializer);
        }
        return new Values<TValueType>(segment, 0, 0, Serializer);
    }

    /// <inheritdoc />
    public void Add(IEntity entity, TValueType value)
    {
        entity.Tx!.Add(entity.Id, this, value);
    }

    /// <summary>
    /// Write a datom for this attribute to the given writer
    /// </summary>
    public void Write<TWriter>(EntityId entityId, RegistryId registryId, TValueType value, TxId txId, bool isRetract, TWriter writer)
    where TWriter : IBufferWriter<byte>
    {
        var prefix = new KeyPrefix().Set(entityId, GetDbId(registryId), txId, isRetract);
        var span = writer.GetSpan(KeyPrefix.Size);
        MemoryMarshal.Write(span, prefix);
        writer.Advance(KeyPrefix.Size);

        var serializer = Serializer;
        if (serializer == null)
            throw new NullReferenceException("Serializer is not set for attribute " + Id);

        Serializer.Serialize(value, writer);
    }

    /// <summary>
    ///     Typed datom for this attribute
    /// </summary>
    public readonly record struct ReadDatom : IReadDatom
    {
        private readonly ulong _tx;

        /// <summary>
        ///     The value for this datom
        /// </summary>
        public required TValueType V { get; init; }

        /// <summary>
        ///     The attribute for this datom
        /// </summary>
        public required IAttribute A { get; init; }

        /// <summary>
        ///     The entity id for this datom
        /// </summary>
        public required EntityId E { get; init; }

        /// <summary>
        ///     The transaction id for this datom
        /// </summary>
        public TxId T
        {
            get => TxId.From(_tx >> 1);
            init => _tx = (_tx & 1) | (value.Value << 1);
        }

        /// <inheritdoc />
        public bool IsRetract
        {
            get => (_tx & 1) == 1;
            init => _tx = (_tx & ~1UL) | (value ? 1UL : 0);
        }

        /// <inheritdoc />
        public object ObjectValue => V!;

        /// <inheritdoc />
        public Type ValueType => typeof(TValueType);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({(IsRetract ? "-" : "+")}, {E.Value:x}, {A.Id.Name}, {V}, {T.Value:x})";
        }
    }

}
