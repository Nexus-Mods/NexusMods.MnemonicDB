namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A node that can be appended to.
/// </summary>
public interface IAppendableNode
{
    /// <summary>
    /// Appends the new data to the node. Using the given serializer to serialize the value.
    /// </summary>
    public void Append<TValue>(EntityId e, AttributeId a, TxId t, DatomFlags f, IValueSerializer<TValue> serializer,
        TValue value);
}
