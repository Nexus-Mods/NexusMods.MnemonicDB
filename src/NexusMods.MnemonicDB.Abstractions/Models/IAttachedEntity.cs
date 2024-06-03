namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// An entity that may be attached to a transaction.
/// </summary>
public interface IAttachedEntity : IHasEntityIdAndDb, IHasTransaction;
