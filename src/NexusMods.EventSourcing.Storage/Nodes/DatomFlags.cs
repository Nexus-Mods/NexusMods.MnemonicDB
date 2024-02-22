using System;

namespace NexusMods.EventSourcing.Storage.Nodes;

[Flags]
public enum DatomFlags : byte
{
    Added = 0x01,
    InlinedData = 0x02,
}
