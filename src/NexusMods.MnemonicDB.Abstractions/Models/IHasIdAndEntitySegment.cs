namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// An interface for referring to things that have an id and an index segment
/// </summary>
public interface IHasIdAndEntitySegment : IHasEntityIdAndDb
{
    /// <summary>
    /// The index segment for this entity
    /// </summary>
    public Datoms EntitySegment { get; }
}
