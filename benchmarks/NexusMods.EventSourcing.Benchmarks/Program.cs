// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using NexusMods.EventSourcing;
using NexusMods.EventSourcing.Benchmarks;
using NexusMods.EventSourcing.TestModel;


#if DEBUG
var readBenchmarks = new ReadBenchmarks();
readBenchmarks.EventStoreType = typeof(InMemoryEventStore<EventSerializer>);
readBenchmarks.EventCount = 10;
readBenchmarks.EntityCount = 10;
await readBenchmarks.Setup();
readBenchmarks.ReadEvents();
#else
BenchmarkRunner.Run<ReadBenchmarks>();
#endif
