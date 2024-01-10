﻿// See https://aka.ms/new-console-template for more information

using System;
using BenchmarkDotNet.Running;
using NexusMods.EventSourcing.Benchmarks;
using NexusMods.EventSourcing.RocksDB;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel;


/*
#if DEBUG
var readBenchmarks = new EntityContextBenchmarks();
readBenchmarks.EventStoreType = typeof(RocksDBEventStore<BinaryEventSerializer>);
readBenchmarks.EventCount = 1000;
readBenchmarks.EntityCount = 1000;
Console.WriteLine("Setup");
readBenchmarks.Setup();
Console.WriteLine("LoadAllEntities");
readBenchmarks.LoadAllEntities();
Console.WriteLine("LoadAllEntities done");
#else
BenchmarkRunner.Run<EntityContextBenchmarks>();
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

/*
| Method      | Mean     | Error   | StdDev  | Gen0   | Allocated |
|------------ |---------:|--------:|--------:|-------:|----------:|
| Serialize   | 174.2 ns | 0.42 ns | 0.37 ns |      - |         - |
| Deserialize | 133.7 ns | 0.80 ns | 0.75 ns | 0.0312 |     592 B |


*/
