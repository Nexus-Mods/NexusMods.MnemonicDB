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
    /// <param name="guid"></param>
    protected ScalarAttribute(string uniqueName = "")
    {
        if (uniqueName == "")
            uniqueName = typeof(TAttribute).FullName!;
        Id = Symbol.Intern(uniqueName);
    }

    /// <summary>
    /// Create a new attribute from an already parsed guid
    /// </summary>
    /// <param name="guid"></param>
    protected ScalarAttribute(Symbol symbol)
    {
        Id = symbol;
    }

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

    public IDatom Read(ulong entity, ulong tx, bool isAssert, ReadOnlySpan<byte> buffer)
    {
        _serializer.Read(buffer, out var val);
        return isAssert
            ? new AssertDatomWithTx<TAttribute, TValueType>(entity, val, TxId.From(tx))
            : throw new NotImplementedException();
    }

    public static IDatom Assert(ulong e, TValueType v)
    {
        return new AssertDatom<TAttribute, TValueType>(e, v);
    }

}
