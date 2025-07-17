using System;

namespace NexusMods.HyperDuck.Adaptor;

/// <summary>
/// A flyweight implementation of a row, internally this will reference the underlying vectors of a chunk
/// </summary>
public ref struct RowCursor
{
    public int RowIndex;
    private readonly ReadOnlySpan<ReadOnlyVector> _vectors;

    public RowCursor(ReadOnlySpan<ReadOnlyVector> vectors)
    {
        RowIndex = 0;
        _vectors = vectors;
    }
}
