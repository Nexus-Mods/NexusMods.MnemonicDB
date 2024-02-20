using System;

namespace NexusMods.EventSourcing.Abstractions;

public interface IDatom
{
    ulong E { get; }
    Type Attribute { get; }
    Type ValueType { get; }
    void Emit<TSink>(ref TSink sink) where TSink : IDatomSink;

    /// <summary>
    /// The datom should call the remap function on each entity id it contains
    /// to remap the entity ids to actual ids
    /// </summary>
    /// <param name="remapFn"></param>
    void Remap(Func<EntityId, EntityId> remapFn);
}

public interface IDatomWithTx : IDatom
{
    TxId Tx { get; }
}

public class AssertDatom<TAttr, TVal>(ulong e, TVal v) : IDatom
    where TAttr : IAttribute<TVal>
{
    public ulong E => e;

    public TVal V => v;
    public Type Attribute => typeof(TAttr);
    public Type ValueType => typeof(TVal);
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
    /// <param name="e"></param>
    /// <param name="v"></param>
    /// <param name="tx"></param>
    public AssertDatomWithTx(ulong e, TVal v, TxId tx) : base(e, v)
    {
        Tx = tx;
    }

    public override string ToString()
    {
        return $"(assert! {E}, {Attribute.Namespace}/{Attribute.Name}, {V}, {Tx})";
    }
}


public static class Datom {
    public static IDatom Assert<TAttr, TVal>(ulong e, TVal v) where TAttr : IAttribute<TVal>
    {
        return new AssertDatom<TAttr, TVal>(e, v);
    }
}
