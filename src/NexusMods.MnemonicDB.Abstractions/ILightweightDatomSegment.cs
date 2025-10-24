using System;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB.Abstractions;

public interface ILightweightDatomSegment : IDisposable, ISpanDatomLikeRO
{
    /// <summary>
    /// Move to the next datom in the segment, returns false if there are no more datoms
    /// </summary>
    public bool MoveNext();

    /// <summary>
    /// Attempts to fast forward to the next datom that equals the given EntityId
    /// </summary>
    bool FastForwardTo(EntityId from);
}
