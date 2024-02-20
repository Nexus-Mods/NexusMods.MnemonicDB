// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;
using BenchmarkDotNet.Running;
using NexusMods.EventSourcing.Benchmarks.Benchmarks;


#if DEBUG

var benchmark = new ReadTests();
benchmark.Count = 1000;

var sw = Stopwatch.StartNew();
benchmark.Setup();
for (var i = 0; i < 1000; i++)
{
    benchmark.ReadFiles();
}
Console.WriteLine("Elapsed: " + sw.Elapsed);
#else

BenchmarkRunner.Run<ReadTests>();

#endif
