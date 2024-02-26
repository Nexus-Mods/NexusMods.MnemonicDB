using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IAppendableChunk
{
    public void Append(in Datom datom);
}
