using BenchmarkDotNet.Attributes;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Benchmarks;

public class AppendableChunkBenchmarks
{


    [Params(1, 128, 1024)]
    public ulong EntityCount { get; set; }

    public void AppendtoChunk()
    {
        var chunk = new AppendableChunk();
        for (ulong i = 0; i < EntityCount; i++)
        {
            chunk.Append(new Datom(EntityId.From(i), AttributeId.From(i), TxId.From(i, DatomFlags.None, new byte[] { 0, 1, 2, 3 }));
        }

    }
}
