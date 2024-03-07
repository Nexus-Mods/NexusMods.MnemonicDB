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
ulong result = 0;
for (var i = 0; i < 1000000; i++)
{
    result = benchmark.ReadFiles();
}
Console.WriteLine("Elapsed: " + sw.Elapsed + " Result: " + result);
#else

BenchmarkRunner.Run<ReadTests>();

#endif
