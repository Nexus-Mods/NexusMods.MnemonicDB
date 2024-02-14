using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A sink for datoms that come from the transaction log.
/// </summary>
public interface ITxLogDatomSink
{
    public bool Emit(ulong e, uint a, ReadOnlySpan<byte> v, ulong tx, bool isRetract);
}
