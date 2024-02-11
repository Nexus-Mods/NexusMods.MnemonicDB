namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents an iterator over a set of datoms.
/// </summary>
public interface IEntityIterator
{
    /// <summary>
    /// Move to the next datom for the current entity
    /// </summary>
    /// <returns></returns>
    public bool Next();

    /// <summary>
    /// Sets the current entity id, this implicitly resets the iterator.
    /// </summary>
    /// <param name="entityId"></param>
    public void SetEntityId(EntityId entityId);

    /// <summary>
    /// Gets the current datom as a distinct value.
    /// </summary>
    public IDatom Current { get; }
}
