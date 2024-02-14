namespace NexusMods.EventSourcing.Abstractions;

public interface IDatomSink
{
    public void Datom<TAttr, TVal>(ulong e, TVal v, bool isAssert)
    where TAttr : IAttribute<TVal>;
}
