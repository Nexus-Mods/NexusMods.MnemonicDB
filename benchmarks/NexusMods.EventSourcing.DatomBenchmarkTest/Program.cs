// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;
using NexusMods.EventSourcing.DatomBenchmarkTest;
using NexusMods.Paths;

var sw = Stopwatch.StartNew();
var db = new DatomStore($"c:\\tmp\\testData{Guid.NewGuid()}.rocksdb");
var data = new GenerateData(db);
data.Generate();
Console.WriteLine($"Elapsed: {sw.Elapsed} Emitted: {data.EmitCount}");
