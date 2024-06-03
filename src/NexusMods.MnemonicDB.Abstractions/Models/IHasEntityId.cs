namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Defines something that has an entity id.
/// </summary>
public interface IHasEntityId
{
    /// <summary>
    /// The entity id.
    /// </summary>
    public EntityId Id { get; }
}
