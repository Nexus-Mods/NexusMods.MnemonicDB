using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IAppendableBlobColumn : IBlobColumn
{
    public void Append(ReadOnlySpan<byte> value);

}
