namespace NexusMods.EventSourcing.Abstractions;

public interface ITransaction
{
    public void Emit<TAttr, TValue>(EntityId entityId, IAttribute<TValue> attribute, TValue value)
        where TAttr : IAttribute;
}
