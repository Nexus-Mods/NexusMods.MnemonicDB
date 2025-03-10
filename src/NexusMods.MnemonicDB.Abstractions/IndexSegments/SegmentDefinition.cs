using System;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

public class SegmentDefinition
{
    private readonly IColumn[] _columns;

    public SegmentDefinition(params IColumn[] columns)
    {
        _columns = columns;
    }
    
}
