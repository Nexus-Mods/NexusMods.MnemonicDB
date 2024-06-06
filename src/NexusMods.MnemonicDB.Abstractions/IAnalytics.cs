using System.Collections.Frozen;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Database analytics, attached to each IDb instance but often calculated on-the fly
/// and cached.
/// </summary>
public interface IAnalytics
{
    /// <summary>
    /// All the entities referenced in the most recent transaction of the database.
    /// </summary>
    public FrozenSet<EntityId> LatestTxIds { get; }
}
