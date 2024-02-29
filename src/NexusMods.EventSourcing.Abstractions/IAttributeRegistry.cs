namespace NexusMods.EventSourcing.Abstractions;

public interface IAttributeRegistry
{
    public void Append<TAttribute, TValue>(IAppendableChunk chunk, EntityId e, TValue value, TxId t, DatomFlags f)
        where TAttribute : IAttribute<TValue>;
}
