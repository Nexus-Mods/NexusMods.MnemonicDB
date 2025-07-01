using System;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

public interface ILightweightDatomSegment : IDisposable
{
    public KeyPrefix KeyPrefix { get; }
    
    public ReadOnlySpan<byte> ValueSpan { get; }
    
    public ReadOnlySpan<byte> ExtraValueSpan { get; }

    /// <summary>
    /// Move to the next datom in the segment, returns false if there are no more datoms
    /// </summary>
    public bool MoveNext();

    /// <summary>
    /// Assuming the segment is ordered by entity id, fast forward to the next
    /// datom with the given entity id. Returns false if the iterator has ended, or
    /// the current entity id greater than the given entity id. If the current value
    /// is already greater or equal to the given entity id, it will not move forward.
    /// </summary>
    bool FastForwardTo(EntityId eid);
}
