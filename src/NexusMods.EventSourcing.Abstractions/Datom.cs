using System;

namespace NexusMods.EventSourcing.Abstractions;

public interface IDatom
{
    ulong E { get; }
    Type Attribute { get; }
    Type ValueType { get; }
    void Emit<TSink>(ref TSink sink) where TSink : IDatomSink;
}

public class AssertDatom<TAttr, TVal>(ulong e, TVal v) : IDatom
    where TAttr : IAttribute<TVal>
{
    public ulong E => e;
    public Type Attribute => typeof(TAttr);
    public Type ValueType => typeof(TVal);
    public void Emit<TSink>(ref TSink sink) where TSink : IDatomSink
    {
        sink.Datom<TAttr, TVal>(e, v, true);
    }
}

public static class Datom {
    public static IDatom Assert<TAttr, TVal>(ulong e, TVal v) where TAttr : IAttribute<TVal>
    {
        return new AssertDatom<TAttr, TVal>(e, v);
    }
}
