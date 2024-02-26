using System;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IAppendableBlobColumn : IBlobColumn
{
    public void Append(ReadOnlySpan<byte> value);

    public void Swap(int idx1, int idx2);
}
