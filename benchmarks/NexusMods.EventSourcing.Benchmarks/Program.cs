// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using NexusMods.EventSourcing;
using NexusMods.EventSourcing.Benchmarks;
using NexusMods.EventSourcing.FasterKV;
using NexusMods.EventSourcing.TestModel;


#if DEBUG
var readBenchmarks = new ReadBenchmarks();
readBenchmarks.EventStoreType = typeof(FasterKVEventStore<EventSerializer>);
readBenchmarks.EventCount = 10000;
readBenchmarks.EntityCount = 10000;
readBenchmarks.Setup();
readBenchmarks.ReadEvents();
#else
BenchmarkRunner.Run<ReadBenchmarks>();
#endif
