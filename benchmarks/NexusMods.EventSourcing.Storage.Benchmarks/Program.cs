// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using NexusMods.EventSourcing.Storage.Benchmarks;
using NexusMods.EventSourcing.Storage.Nodes;

//BenchmarkRunner.Run<AppendableChunkBenchmarks>();


//
var benchmark = new AppendableChunkBenchmarks();
benchmark.EntityCount = 1024;
benchmark.IterationSetup();

for(int i = 0; i < 1; i++)
    benchmark.SortChunk();

