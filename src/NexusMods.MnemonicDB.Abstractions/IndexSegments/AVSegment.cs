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
    
    /// <summary>
    /// Gets the attribute IDs column
    /// </summary>
    public ReadOnlySpan<AttributeId> GetAttributeIds() => this.GetValues1<AVSegment, AttributeId>();
    
    /// <summary>
    /// Gets the value types column
    /// </summary>
    public ReadOnlySpan<ValueTag> GetValueTypes() => this.GetValues2<AVSegment, AttributeId, ValueTag>();
    
    /// <summary>
    /// Gets the offsets column
    /// </summary>
    public ReadOnlySpan<Offset> GetOffsets() => this.GetValues3<AVSegment, AttributeId, ValueTag, Offset>();
    
    /// <summary>
    /// Builds the segment of this type from the given builder
    /// </summary>
    public static Memory<byte> Build(in IndexSegmentBuilder builder)
    {
        return builder.Build<AttributeId, ValueTag, Offset>();
    }
}
