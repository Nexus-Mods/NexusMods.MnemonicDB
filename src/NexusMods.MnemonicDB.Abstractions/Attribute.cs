using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
/// <typeparam name="TValueType"></typeparam>
/// <typeparam name="TAttribute"></typeparam>
public class Attribute<TAttribute, TValueType> : IAttribute<TValueType>
    where TAttribute : IAttribute<TValueType>
{
    private IValueSerializer<TValueType> _serializer = null!;

    /// <summary>
    ///     Create a new attribute
    /// </summary>
    protected Attribute(string uniqueName = "",
        bool isIndexed = false,
        bool noHistory = false,
        bool multiValued = false)
    {
        IsIndexed = isIndexed;
        NoHistory = noHistory;
        Multivalued = multiValued;
        Id = uniqueName == "" ? Symbol.Intern(typeof(TAttribute).FullName!) : Symbol.InternPreSanitized(uniqueName);
    }

    /// <summary>
    ///     Create a new attribute from an already parsed guid
    /// </summary>
    protected Attribute(Symbol symbol)
    {
        Id = symbol;
    }

    public bool Multivalued { get; }

    /// <inheritdoc />
    public bool IsIndexed { get; }

    public bool NoHistory { get; }
    IValueSerializer IAttribute.Serializer => _serializer;


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
        return new ReadDatom
        {
            E = entityId,
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
    /// <param name="e"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    public static IWriteDatom Assert(EntityId e, TValueType v)
    {
        return new WriteDatom
        {
            E = e,
            V = v,
            IsRetract = false
        };
    }

    public static IWriteDatom Retract(EntityId e, TValueType v)
    {
        return new WriteDatom
        {
            E = e,
            V = v,
            IsRetract = true
        };
    }

    /// <inheritdoc />
    public IValueSerializer<TValueType> Serializer => _serializer;

    private class InlineCache
    {
        public required IAttributeRegistry Registry = null!;
        public required AttributeId Id;
        public required IValueSerializer<TValueType> Serializer = null!;
        public required TAttribute Attribute = default!;
    }

    private static InlineCache _cache = new ()
    {
        Registry = null!,
        Id = default!,
        Serializer = null!,
        Attribute = default!
    };

    /// <summary>
    /// Gets the value for this attribute on the given entity
    /// </summary>
    public static TValueType Get(IEntity entity)
    {
        var segment = entity.Db.GetSegment(entity.Id);
        var cache = GetInlinedCache(entity);
        for (var i = 0; i < segment.Count; i++)
        {
            var datom = segment[i];
            if (datom.A == cache.Id)
            {
                return datom.Resolve<TValueType>();
            }
        }
        ThrowKeyNotFoundException(entity.Id);
        return default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static InlineCache GetInlinedCache(IEntity entity)
    {
        var cache = _cache;
        if (!ReferenceEquals(cache.Registry, entity.Db.Registry))
        {
            cache = FillInlineCache(entity);
        }
        return cache;
    }

    private static InlineCache FillInlineCache(IEntity entity)
    {
        InlineCache cache;
        var attribute = entity.Db.Registry.GetAttribute<TAttribute>();
        var attrId = entity.Db.Registry.GetAttributeId(typeof(TAttribute));
        cache = new InlineCache
        {
            Registry = entity.Db.Registry,
            Id = attrId,
            Serializer = attribute.Serializer,
            Attribute = attribute
        };
        _cache = cache;
        return cache;
    }

    private static void ThrowKeyNotFoundException(EntityId id)
    {
        throw new KeyNotFoundException($"Attribute {typeof(TAttribute).Name} not found on entity {id}");
    }

    /// <summary>
    /// Gets all values for this attribute on the given entity
    /// </summary>
    public static Values<TValueType> GetAll(IEntity ent)
    {
        var segment = ent.Db.GetSegment(ent.Id);
        var cache = GetInlinedCache(ent);
        for (var i = 0; i < segment.Count; i++)
        {
            var datom = segment[i];
            if (datom.A != cache.Id) continue;

            var start = i;
            while (i < segment.Count && segment[i].A == cache.Id)
            {
                i++;
            }
            return new Values<TValueType>(segment, start, i, cache.Serializer);
        }
        return new Values<TValueType>(segment, 0, 0, cache.Serializer);
    }

    /// <inheritdoc />
    public static void Add(IEntity entity, TValueType value)
    {
        entity.Tx!.Add<TAttribute, TValueType>(entity.Id, value);
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
            return $"({E.Value:x}, {typeof(TAttribute).Name}, {V}, {T.Value:x})";
        }
    }
}
