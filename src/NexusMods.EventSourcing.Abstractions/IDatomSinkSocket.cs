namespace NexusMods.EventSourcing.Abstractions;

public interface IDatomSinkSocket
{
    public void Process<TSink>(ref TSink sink) where TSink : IDatomSink;
}
