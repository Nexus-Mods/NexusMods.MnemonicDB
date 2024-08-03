using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions.Models;

public interface IHasIdAndIndexSegment : IHasEntityIdAndDb
{
    public IndexSegment IndexSegment { get; }
}
