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
    /// Read a datom from a buffer
    /// </summary>
    public IDatom Read(ulong entity, ulong tx, bool isAssert, ReadOnlySpan<byte> buffer)
    {
        _serializer.Read(buffer, out var val);
        return isAssert
            ? new AssertDatomWithTx<TAttribute, TValueType>(entity, val, TxId.From(tx))
            : throw new NotImplementedException();
    }

    /// <summary>
    /// Create a new datom for an assert on this attribute, and return it
    /// </summary>
    /// <param name="e"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    public static IDatom Assert(ulong e, TValueType v)
    {
        return new AssertDatom<TAttribute, TValueType>(e, v);
    }

}
