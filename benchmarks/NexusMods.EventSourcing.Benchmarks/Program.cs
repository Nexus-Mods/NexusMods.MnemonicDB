// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;
using BenchmarkDotNet.Running;
using NexusMods.EventSourcing.Benchmarks.Benchmarks;


#if DEBUG

var benchmark = new WriteTests();
benchmark.Count = 1000;

var sw = Stopwatch.StartNew();
for (var i = 0; i < 1000; i++)
{
    benchmark.AddFiles();
}
Console.WriteLine("Elapsed: " + sw.Elapsed);
#else

BenchmarkRunner.Run<WriteTests>();

#endif
