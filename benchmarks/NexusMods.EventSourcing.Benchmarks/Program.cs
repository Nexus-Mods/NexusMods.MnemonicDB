// See https://aka.ms/new-console-template for more information

using System;
using BenchmarkDotNet.Running;
using NexusMods.EventSourcing;
using NexusMods.EventSourcing.Benchmarks;
using NexusMods.EventSourcing.FasterKV;
using NexusMods.EventSourcing.RocksDB;
using NexusMods.EventSourcing.TestModel;


#if DEBUG
var readBenchmarks = new EntityContextBenchmarks();
readBenchmarks.EventStoreType = typeof(RocksDBEventStore<EventSerializer>);
readBenchmarks.EventCount = 10;
readBenchmarks.EntityCount = 10;
Console.WriteLine("Setup");
readBenchmarks.Setup();
Console.WriteLine("LoadAllEntities");
readBenchmarks.LoadAllEntities();
Console.WriteLine("LoadAllEntities done");
#else
BenchmarkRunner.Run<EntityContextBenchmarks>();
#endif
