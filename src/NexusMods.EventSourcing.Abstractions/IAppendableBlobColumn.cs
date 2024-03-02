using System;

namespace NexusMods.EventSourcing.Abstractions;

public interface IAppendableBlobColumn : IBlobColumn
{
    public void Append(ReadOnlySpan<byte> value);

}
