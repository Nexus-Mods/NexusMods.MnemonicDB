using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents a single fact in the database. Consists of an entity, attribute, value, and transaction id.
/// </summary>
public interface IDatom
{
    /// <summary>
    /// The entity id of the datom
    /// </summary>
    ulong Entity { get; }

    /// <summary>
    /// The attribute of the datom
    /// </summary>
    Type Attribute { get; }

    /// <summary>
    /// The value of the datom
    /// </summary>
    Type ValueType { get; }

    /// <summary>
    /// Sends the datom to the sink
    /// </summary>
    /// <param name="sink"></param>
    /// <typeparam name="TSink"></typeparam>
    void Emit<TSink>(ref TSink sink) where TSink : IDatomSink;

    /// <summary>
    /// The datom should call the remap function on each entity id it contains
    /// to remap the entity ids to actual ids
    /// </summary>
    /// <param name="remapFn"></param>
    void Remap(Func<EntityId, EntityId> remapFn);
}

/// <summary>
/// A datom that includes a transaction id
/// </summary>
public interface IDatomWithTx : IDatom
{
    /// <summary>
    /// The transaction id of the datom
    /// </summary>
    TxId Tx { get; }
}


/// <summary>
/// An assertion datom, most datoms are assertions, so these are called out in a specific class
/// here to save memory
/// </summary>
/// <param name="e"></param>
/// <param name="v"></param>
/// <typeparam name="TAttr"></typeparam>
/// <typeparam name="TVal"></typeparam>
public class AssertDatom<TAttr, TVal>(ulong e, TVal v) : IDatom
    where TAttr : IAttribute<TVal>
{
    /// <inheritdoc />
    public ulong Entity => e;

    /// <summary>
    /// The value of the datom
    /// </summary>
    public TVal V => v;

    /// <inheritdoc />
    public Type Attribute => typeof(TAttr);

    /// <inheritdoc />
    public Type ValueType => typeof(TVal);

    /// <inheritdoc />
    public void Emit<TSink>(ref TSink sink) where TSink : IDatomSink
    {
        sink.Datom<TAttr, TVal>(e, v, true);
    }

    /// <inheritdoc />
    public void Remap(Func<EntityId, EntityId> remapFn)
    {
        e = remapFn(EntityId.From(e)).Value;
        if (v is EntityId entityId)
        {
            v = (TVal) (object) EntityId.From(remapFn(entityId).Value);
        }
    }
}

/// <summary>
/// An assertion datom that has a transaction id
/// </summary>
/// <typeparam name="TAttr"></typeparam>
/// <typeparam name="TVal"></typeparam>
public class AssertDatomWithTx<TAttr, TVal> : AssertDatom<TAttr, TVal>, IDatomWithTx
    where TAttr : IAttribute<TVal>
{
    /// <inheritdoc />
    public TxId Tx { get; }

    /// <summary>
    /// Default Constructor
    /// </summary>
    public AssertDatomWithTx(ulong e, TVal v, TxId tx) : base(e, v)
    {
        Tx = tx;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"(assert! {Entity}, {Attribute.Namespace}/{Attribute.Name}, {V}, {Tx})";
    }
}
