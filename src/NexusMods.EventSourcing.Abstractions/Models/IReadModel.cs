namespace NexusMods.EventSourcing.Abstractions.Models;

/// <summary>
/// Base interface for all read models. The AReadModel class takes a generic parameter
/// which makes it hard to use as a base class for all read models in cases where the user
/// doesn't care about the type of the read model.
/// </summary>
public interface IReadModel
{
    /// <summary>
    /// The entity id of the read model.
    /// </summary>
    public EntityId Id { get; }

    /// <summary>
    /// The attached db instance of the read model.
    /// </summary>
    public IDb Db { get; }
}
