using System;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

public struct ESegment : ISegment<EntityId>
{
    public ReadOnlyMemory<byte> Data { get; }
}
