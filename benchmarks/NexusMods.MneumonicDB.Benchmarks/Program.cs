// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;
using NexusMods.MneumonicDB.Benchmarks.Benchmarks;


//#if DEBUG

var benchmark = new ReadTests
{
    Count = 128
};

var sw = Stopwatch.StartNew();
await benchmark.Setup();
long result = 0;
for (var i = 0; i < 10000; i++) result = benchmark.ReadAll();
Console.WriteLine("Elapsed: " + sw.Elapsed + " Result: " + result);


/*
#else

BenchmarkRunner.Run<ReadTests>();

#endif
*/
