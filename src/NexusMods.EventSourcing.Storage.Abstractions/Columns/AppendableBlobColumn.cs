using System;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Storage.Abstractions.Columns;

public class AppendableBlobColumn : IAppendableBlobColumn
{
    private readonly List<byte[]> _values = new();

    public ReadOnlyMemory<byte> this[int idx] => _values[idx];

    public int Length => _values.Count;

    public void Append(ReadOnlySpan<byte> value)
    {
        _values.Add(value.ToArray());
    }

    public void Swap(int idx1, int idx2)
    {
        (_values[idx1], _values[idx2]) = (_values[idx2], _values[idx1]);
    }
}
