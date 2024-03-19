using System;

namespace NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

public interface IChunkIterator : IDisposable
{
    /// <summary>
    /// Moves to the next chunk in the result set, returns false if there are no more chunks.
    /// </summary>
    /// <returns></returns>
    public bool Next();

    public DatomChunk Current { get; }
}
