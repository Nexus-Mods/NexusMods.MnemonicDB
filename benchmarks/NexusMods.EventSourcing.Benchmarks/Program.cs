// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;
using BenchmarkDotNet.Running;
using NexusMods.EventSourcing.Benchmarks.Benchmarks;


#if DEBUG

var benchmark = new ReadTests
{
    Count = 128
};

var sw = Stopwatch.StartNew();
await benchmark.Setup();
for (var i = 0; i < 1000000; i++)
{
    benchmark.ReadFiles();
}
Console.WriteLine("Elapsed: " + sw.Elapsed);
#else

BenchmarkRunner.Run<ReadTests>();

#endif
