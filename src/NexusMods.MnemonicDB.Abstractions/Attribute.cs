using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
/// <typeparam name="TValueType"></typeparam>
public sealed class Attribute<TValueType>
    : IAttribute<TValueType>
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

    public bool IsIndexed { get; }

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


    /// <summary>
    ///     Create a new datom for an assert on this attribute, and return it
    /// </summary>
    public IWriteDatom Assert(EntityId e, TValueType v)
    {
        return new WriteDatom
        {
            E = e,
            Attribute = this,
            V = v,
            IsRetract = false
        };
    }

    public IWriteDatom Retract(EntityId e, TValueType v)
    {
        return new WriteDatom
        {
            E = e,
            Attribute = this,
            V = v,
            IsRetract = true
        };
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
    ///     Typed datom for this attribute
    /// </summary>
    public readonly record struct WriteDatom : IWriteDatom
    {
        /// <summary>
        ///     The value for this datom
        /// </summary>
        public required TValueType V { get; init; }

        /// <summary>
        ///     The entity id for this datom
        /// </summary>
        public required EntityId E { get; init; }


        public required Attribute<TValueType> Attribute { get; init; }

        /// <summary>
        ///     True if this is a retraction
        /// </summary>
        public required bool IsRetract { get; init; }

        public void Explode<TWriter>(IAttributeRegistry registry, Func<EntityId, EntityId> remapFn,
            out EntityId e, out AttributeId a, TWriter vWriter, out bool isRetract)
            where TWriter : IBufferWriter<byte>
        {
            isRetract = IsRetract;
            e = EntityId.From(Ids.IsPartition(E.Value, Ids.Partition.Tmp) ? remapFn(E).Value : E.Value);

            if (V is EntityId id)
            {
                var newId = remapFn(id);
                if (newId is TValueType recasted)
                {
                    throw new NotImplementedException();
                    //registry.Explode<TValueType, TWriter>(out a, recasted, vWriter);
                    return;
                }
            }

            throw new NotImplementedException();
            //registry.Explode<TValueType, TWriter>(out a, V, vWriter);
        }


        /// <inheritdoc />
        public override string ToString()
        {
            return $"({E.Value:x}, {Attribute.Id.Name}, {V})";
        }
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
        public required Attribute<TValueType> A { get; init; }

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
            return $"({E.Value:x}, {A.Id.Name}, {V}, {T.Value:x})";
        }
    }
}
