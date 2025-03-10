using System;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

[Flags]
public enum SegmentPart
{
    Entity = 1,
    Attribute = 2,
    Value = 4,
    Tx = 8,
    IsRetract = 16,
    IndexType = 32,
}
