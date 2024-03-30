﻿using System;
using System.Buffers;
using NexusMods.MneumonicDB.Abstractions.Internals;
using NexusMods.MneumonicDB.Abstractions.Models;

namespace NexusMods.MneumonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
/// <typeparam name="TValueType"></typeparam>
/// <typeparam name="TAttribute"></typeparam>
public class ScalarAttribute<TAttribute, TValueType> : IAttribute<TValueType>
    where TAttribute : IAttribute<TValueType>
{
    private IValueSerializer<TValueType> _serializer = null!;

    /// <summary>
    ///     Create a new attribute
    /// </summary>
    protected ScalarAttribute(string uniqueName = "",
        bool isIndexed = false,
        bool keepHistory = true,
        bool multiArity = false)
    {
        IsIndexed = isIndexed;
        KeepHistory = keepHistory;
        MultiArity = multiArity;
        Id = uniqueName == "" ? Symbol.Intern(typeof(TAttribute).FullName!) : Symbol.InternPreSanitized(uniqueName);
    }

    /// <summary>
    ///     Create a new attribute from an already parsed guid
    /// </summary>
    protected ScalarAttribute(Symbol symbol)
    {
        Id = symbol;
    }

    public bool MultiArity { get; }

    public bool KeepHistory { get; }

    /// <inheritdoc />
    public bool IsIndexed { get; }


    /// <inheritdoc />
    public static void Add(ITransaction tx, EntityId entity, TValueType value)
    {
        tx.Add<TAttribute, TValueType>(entity, value);
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
    public bool IsMultiCardinality => false;

    /// <inheritdoc />
    public bool IsReference => typeof(TValueType) == typeof(EntityId);

    /// <inheritdoc />
    public Symbol Id { get; }

    /// <inheritdoc />
    public IReadDatom Resolve(EntityId entityId, AttributeId attributeId, ReadOnlySpan<byte> value, TxId tx,
        bool isRetract)
    {
        _serializer.Read(value, out var val);
        return new ReadDatom
        {
            E = entityId,
            V = val,
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
    /// <param name="e"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    public static IWriteDatom Assert(EntityId e, TValueType v)
    {
        return new WriteDatom
        {
            E = e,
            V = v
        };
    }

    public static TValueType Get<TReadModel>(in TReadModel model)
        where TReadModel : IEntity
    {
        return model.Db.Get<TAttribute, TValueType>(model.Id);
    }

    /// <inheritdoc />
    public static void Add<TReadModel>(in TReadModel model, TValueType value)
        where TReadModel : IEntity
    {
        tx.Add<TAttribute, TValueType>(model.Id, value);
    }

    /// <inheritdoc />
    public TValueType Read(ReadOnlySpan<byte> buffer)
    {
        _serializer.Read(buffer, out var val);
        return val;
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

        public void Explode<TWriter>(IAttributeRegistry registry, Func<EntityId, EntityId> remapFn,
            out EntityId e, out AttributeId a, TWriter vWriter, out bool isRetract)
            where TWriter : IBufferWriter<byte>
        {
            isRetract = false;
            e = EntityId.From(Ids.IsPartition(E.Value, Ids.Partition.Tmp) ? remapFn(E).Value : E.Value);

            if (V is EntityId id)
            {
                var newId = remapFn(id);
                if (newId is TValueType recasted)
                {
                    registry.Explode<TAttribute, TValueType, TWriter>(out a, recasted, vWriter);
                    return;
                }
            }

            registry.Explode<TAttribute, TValueType, TWriter>(out a, V, vWriter);
        }


        /// <inheritdoc />
        public override string ToString()
        {
            return $"({E.Value:x}, {typeof(TAttribute).Name}, {V})";
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

        public object ObjectValue => V!;

        /// <inheritdoc />
        public Type AttributeType => typeof(TAttribute);

        /// <inheritdoc />
        public Type ValueType => typeof(TValueType);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({E}, {typeof(TAttribute).Name}, {V}, {T})";
        }
    }
}
