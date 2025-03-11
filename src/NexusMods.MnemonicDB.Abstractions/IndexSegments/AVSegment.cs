using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

public readonly struct AVSegment : ISegment<AttributeId, ValueTag, Offset>
{
    public ReadOnlyMemory<byte> Data { get; }

    public AVSegment(ReadOnlyMemory<byte> data)
    {
        Data = data;
    }
    
    public ReadOnlySpan<AttributeId> GetAttributeIds() => this.GetValues1<AVSegment, AttributeId>();
    
    public ReadOnlySpan<ValueTag> GetValueTypes() => this.GetValues2<AVSegment, AttributeId, ValueTag>();
    
    public ReadOnlySpan<Offset> GetOffsets() => this.GetValues3<AVSegment, AttributeId, ValueTag, Offset>();
    
    /// <summary>
    /// Builds the segment of this type from the given builder
    /// </summary>
    public static Memory<byte> Build(in IndexSegmentBuilder builder)
    {
        return builder.Build<AttributeId, ValueTag, Offset>();
    }
}
