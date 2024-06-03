namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Represents something that has an entity id and a reference to a database.
/// </summary>
public interface IHasEntityIdAndDb : IHasEntityId, IHasDb;
