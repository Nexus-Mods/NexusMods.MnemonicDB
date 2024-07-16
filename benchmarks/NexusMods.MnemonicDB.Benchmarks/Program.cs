// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using JetBrains.Profiler.Api;
using NexusMods.MnemonicDB.Benchmarks.Benchmarks;


#if DEBUG

using var benchmark = new RealDataBenchmarks();

var sw = Stopwatch.StartNew();
await benchmark.Setup();

ulong result = 0;
Console.WriteLine("Starting benchmark");
MeasureProfiler.StartCollectingData();
//MemoryProfiler.CollectAllocations(true);
for (var i = 0; i < 1; i++)
    result = benchmark.CountHashes();
//MemoryProfiler.CollectAllocations(false);

MeasureProfiler.SaveData();
Console.WriteLine("Elapsed: " + sw.Elapsed + " Result: " + result);


#else

BenchmarkRunner.Run<RealDataBenchmarks>(config: DefaultConfig.Instance.WithOption(ConfigOptions.DisableOptimizationsValidator, true));
#endif



