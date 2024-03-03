namespace NexusMods.EventSourcing.Abstractions;

public interface IAppendableChunk
{
    public void Append<TValue>(EntityId e, AttributeId a, TxId t, DatomFlags f, IValueSerializer<TValue> serializer,
        TValue value);
}
