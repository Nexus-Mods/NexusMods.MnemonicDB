namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Represents an entity identifier, this is normally a TransparentValueObject
/// that wraps a EntityId so that ids can be type safe
/// </summary>
public interface ITypedEntityId
{
    /// <summary>
    /// Get the underlying EntityId
    /// </summary>
    public EntityId Value { get; }
}
