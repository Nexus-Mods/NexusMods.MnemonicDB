namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Defines something that has a reference to a database
/// </summary>
public interface IHasDb
{
    /// <summary>
    /// The database reference
    /// </summary>
    public IDb Db { get; }
}
