using System;
using System.Buffers;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Interface for a specific attribute
/// </summary>
/// <typeparam name="TValueType"></typeparam>
/// <typeparam name="TAttribute"></typeparam>
public class ScalarAttribute<TAttribute, TValueType> : IAttribute<TValueType>
where TAttribute : IAttribute<TValueType>
{
    private IValueSerializer<TValueType> _serializer = null!;

    /// <summary>
    /// Create a new attribute
    /// </summary>
    protected ScalarAttribute(string uniqueName = "")
    {
        Id = uniqueName == "" ?
            Symbol.Intern(typeof(TAttribute).FullName!) :
            Symbol.InternPreSanitized(uniqueName);
    }

    /// <summary>
    /// Create a new attribute from an already parsed guid
    /// </summary>
    protected ScalarAttribute(Symbol symbol)
    {
        Id = symbol;
    }

    /// <inheritdoc />
    public TValueType Read(ReadOnlySpan<byte> buffer)
    {
        _serializer.Read(buffer, out var val);
        return val;
    }


    /// <inheritdoc />
    public static void Add(ITransaction tx, EntityId entity, TValueType value)
    {
        tx.Add<TAttribute, TValueType>(entity, value);
    }

    /// <inheritdoc />
    public void SetSerializer(IValueSerializer serializer)
    {
        if (serializer is not IValueSerializer<TValueType> valueSerializer)
            throw new InvalidOperationException($"Serializer {serializer.GetType()} is not compatible with {typeof(TValueType)}");
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
    public IReadDatom Resolve(Datom datom)
    {
        _serializer.Read(datom.V.Span, out var read);
        return new ReadDatom
        {
            E = datom.E,
            V = read,
            T = datom.T
        };
    }


    /// <inheritdoc />
    public IReadDatom Resolve(EntityId entityId, AttributeId attributeId, ReadOnlySpan<byte> value, TxId tx)
    {
        return new ReadDatom
        {
            E = entityId,
            V = Read(value),
            T = tx
        };
    }

    /// <inheritdoc />
    public Type GetReadDatomType()
    {
        return typeof(ReadDatom);
    }


    /// <summary>
    /// Create a new datom for an assert on this attribute, and return it
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
        };
    }


    /// <summary>
    /// Typed datom for this attribute
    /// </summary>
    public readonly record struct WriteDatom : IWriteDatom
    {
        /// <summary>
        /// The entity id for this datom
        /// </summary>
        public required EntityId E { get; init; }

        /// <summary>
        /// The value for this datom
        /// </summary>
        public required TValueType V { get; init; }


        /// <inheritdoc />
        public override string ToString()
        {
            return $"({E.Value:x}, {typeof(TAttribute).Name}, {V})";
        }

        public void Explode<TWriter>(IAttributeRegistry registry, Func<EntityId, EntityId> remapFn, ref StackDatom datom, TWriter writer) where TWriter : IBufferWriter<byte>
        {
            datom.E = Ids.IsPartition(E.Value, Ids.Partition.Tmp) ? remapFn(E).Value : E.Value;
            registry.Explode<TAttribute, TValueType>(ref datom, V, writer);
        }
    }

    /// <summary>
    /// Typed datom for this attribute
    /// </summary>
    public readonly record struct ReadDatom : IReadDatom
    {
        /// <summary>
        /// The entity id for this datom
        /// </summary>
        public required EntityId E { get; init; }

        /// <summary>
        /// The value for this datom
        /// </summary>
        public required TValueType V { get; init; }

        /// <summary>
        /// The transaction id for this datom
        /// </summary>
        public required TxId T { get; init; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({E}, {typeof(TAttribute).Name}, {V}, {T})";
        }

        /// <inheritdoc />
        public Type AttributeType => typeof(TAttribute);

        /// <inheritdoc />
        public Type ValueType => typeof(TValueType);
    }

}
