using System;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.Algorithms;

public static class ChunkReader
{
    public static IDataChunk ReadDataChunk(ReadOnlyMemory<byte> data)
    {
        var reader = new BufferReader(data);
        var header = reader.ReadFourCC();



        if (header == FourCC.PackedData)
        {
            return PackedChunk.ReadFrom(ref reader);
        }
        else
        {
            throw new InvalidOperationException($"Unknown chunk type: {header}");
        }
    }

}
