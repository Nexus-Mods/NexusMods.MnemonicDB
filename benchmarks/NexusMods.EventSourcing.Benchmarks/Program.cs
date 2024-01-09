// See https://aka.ms/new-console-template for more information

using System;
using BenchmarkDotNet.Running;
using NexusMods.EventSourcing.Benchmarks;
using NexusMods.EventSourcing.RocksDB;
using NexusMods.EventSourcing.TestModel;

/*
#if DEBUG
var readBenchmarks = new EntityContextBenchmarks();
readBenchmarks.EventStoreType = typeof(RocksDBEventStore<EventSerializer>);
readBenchmarks.EventCount = 1000;
readBenchmarks.EntityCount = 1000;
Console.WriteLine("Setup");
readBenchmarks.Setup();
Console.WriteLine("LoadAllEntities");
readBenchmarks.LoadAllEntities();
Console.WriteLine("LoadAllEntities done");
#else
BenchmarkRunner.Run<EventStoreBenchmarks>();
#endif
*/

#if DEBUG
var benchmarks = new AccumulatorBenchmarks();
for (int i = 0; i < 10_000_000; i++)
{
    benchmarks.GetMultiAttributeItems();
}

#else
BenchmarkRunner.Run<AccumulatorBenchmarks>();
#endif
