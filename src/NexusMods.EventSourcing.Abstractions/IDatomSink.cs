namespace NexusMods.EventSourcing.Abstractions;

public interface IDatomSink<TValueType> where TValueType : notnull
{
    public void Datom(ulong e, ulong a, TValueType v, ulong tx, bool isAssert);
}
