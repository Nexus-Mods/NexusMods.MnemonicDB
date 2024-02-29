using System;

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
        if (uniqueName == "")
            uniqueName = typeof(TAttribute).FullName!;
        Id = Symbol.Intern(uniqueName);
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
    public bool IsReference => false;

    /// <inheritdoc />
    public Symbol Id { get; }



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

    public static IReadDatom Datom(EntityId id, TxId tx, TValueType valueType)
    {
        return new ReadDatom
        {
            E = id,
            V = valueType,
            T = tx,
            Flags = DatomFlags.Added
        };
    }

    /// <summary>
    /// Typed datom for this attribute
    /// </summary>
    public readonly record struct WriteDatom : IWriteDatom
    {
        public required EntityId E { get; init; }
        public required TValueType V { get; init; }
        public DatomFlags Flags => DatomFlags.Added;

        public void Append(IAttributeRegistry registry, IAppendableChunk chunk)
        {
            registry.Append<TAttribute, TValueType>(chunk, E, V, TxId.Tmp, Flags);
        }

        public override string ToString()
        {
            return $"({E}, {typeof(TAttribute).Name}, {V})";
        }
    }

    /// <summary>
    /// Typed datom for this attribute
    /// </summary>
    public readonly record struct ReadDatom : IReadDatom
    {
        public required EntityId E { get; init; }
        public required TValueType V { get; init; }

        public required TxId T { get; init; }
        public DatomFlags Flags { get; init; }
        public override string ToString()
        {
            return $"({E}, {typeof(TAttribute).Name}, {V}, {T}, {Flags})";
        }

        public Type AttributeType => typeof(TAttribute);
        public Type ValueType => typeof(TValueType);
    }

}
