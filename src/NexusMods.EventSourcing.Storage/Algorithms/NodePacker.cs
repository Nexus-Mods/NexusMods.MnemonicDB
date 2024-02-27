using System;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.Algorithms;

public static class NodePacker
{
    public static IDataChunk Pack(IDataChunk chunk)
    {
        return new PackedChunk(chunk.Length,
            chunk.EntityIds.Pack(),
            chunk.AttributeIds.Pack(),
            chunk.TransactionIds.Pack(),
            chunk.Flags.Pack(),
            chunk.Values.Pack());
    }
}
