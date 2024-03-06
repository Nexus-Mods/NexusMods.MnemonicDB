// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;
using BenchmarkDotNet.Running;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Benchmarks;
using NexusMods.EventSourcing.Storage.Nodes;

//BenchmarkRunner.Run<IteratorBenchmarks>();


//

var benchmark = new IndexBenchmarks()
{
    Count = 1024,
    SortOrder = SortOrders.EATV,
    TxCount = 256
};
benchmark.GlobalSetup();

var sw = Stopwatch.StartNew();

for (int i = 0; i < 1; i++)
    benchmark.IndexAll();


Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");


