namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A definition for an attribute.
/// </summary>
public interface IAttribute
{

}


/// <summary>
/// A typed definition for an attribute.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IAttribute<T>
{
    void Emit<TTransaction>(EntityId entityId, T value, TTransaction tx)
        where TTransaction : ITransaction;

}
