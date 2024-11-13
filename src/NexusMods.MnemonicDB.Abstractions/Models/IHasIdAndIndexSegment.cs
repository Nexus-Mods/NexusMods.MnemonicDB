using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// An interface for refering to things that have an id and an index segment
/// </summary>
public interface IHasIdAndIndexSegment : IHasEntityIdAndDb
{
    /// <summary>
    /// The index segment for this entity
    /// </summary>
    public IndexSegment IndexSegment { get; }
}
