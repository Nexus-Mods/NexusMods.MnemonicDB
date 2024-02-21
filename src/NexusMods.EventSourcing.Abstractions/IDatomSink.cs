namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A sink is a typed interface that accepts the parts of a datom as values. It is generic and strongly typed
/// so that most of the data is transferred via the stack and does not require boxing. Think of all of this
/// something like IEnumerable, but as a pushed base interface instead of a pulled one.
/// </summary>
public interface IDatomSink
{
    /// <summary>
    /// Inject a datom into the sink.
    /// </summary>
    public void Datom<TAttr, TVal>(ulong e, TVal v, bool isAssert)
    where TAttr : IAttribute<TVal>;
}
