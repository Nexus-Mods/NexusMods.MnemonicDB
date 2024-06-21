﻿// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using JetBrains.Profiler.Api;
using NexusMods.MnemonicDB.Benchmarks.Benchmarks;


#if DEBUG

var benchmark = new ReadTests
{
    Count = 128
};

var sw = Stopwatch.StartNew();
await benchmark.Setup();

ulong result = 0;

MeasureProfiler.StartCollectingData();
//MemoryProfiler.CollectAllocations(true);
for (var i = 0; i < 10000; i++)
    result = benchmark.ReadAllFromMod();
//MemoryProfiler.CollectAllocations(false);

MeasureProfiler.SaveData();
Console.WriteLine("Elapsed: " + sw.Elapsed + " Result: " + result);

#else

BenchmarkRunner.Run<IndexSegmentBenchmarks>(config: DefaultConfig.Instance.WithOption(ConfigOptions.DisableOptimizationsValidator, true));
#endif


