using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.RealisticTestData;
using NexusMods.MnemonicDB.RealisticTestData.Models;
using NexusMods.MnemonicDB.Storage;
using NexusMods.Paths;
using Xunit;
using Xunit.DependencyInjection;

namespace NexusMods.MnemonicDB.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class RealDataBenchmarks : IDisposable
{
    private readonly IHost _host;
    private readonly IServiceProvider _services;
    private readonly IConnection _conn;
    private readonly AbsolutePath _path;
    private Loadout.ReadOnly _loadout;
    private EntityId[] _fileIds = [];

    public RealDataBenchmarks()
    {
        
        _path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("tests_MnemonicDB")
            .Combine(Guid.NewGuid().ToString())
            .Combine("MnemonicDB");
        
        var host = new HostBuilder()
            .ConfigureServices(s =>
            {

                
                _path.CreateDirectory();
                
                s.AddArchiveModel()
                    .AddModModel()
                    .AddLoadoutModel()
                    .AddExtractedFileModel()
                    .AddMnemonicDB()
                    .AddMnemonicDBStorage()
                    .AddRocksDbBackend()
                    .AddSingleton(_ => new DatomStoreSettings
                    {
                        Path = _path
                    });
            });
        _host = host.Build();
        _services = _host.Services;
        _conn = _services.GetRequiredService<IConnection>();
    }

    [GlobalSetup]
    public async Task Setup()
    {
        _loadout = await Root.Import(_conn);
        _fileIds = _loadout.ExtractedFiles
            .IndexSegment.Select(s => s.E)
            .Order()
            .ToArray();
    }

    public void Dispose()
    {
        _host.Dispose();
        _path.DeleteDirectory(recursive:true);
    }

    /*
    [Benchmark]
    public ulong CountHashes()
    {
        var db = _conn.Db;
        ((Db)db).ClearCache();
        
        return (ulong)_loadout.ExtractedFiles
            .Select(ef => ef.Hash)
            .Count();
    }
    */

    [Benchmark]
    public int LoadOneByOne()
    {
        var loaded = 0;
        var db = _conn.Db;
        foreach (var e in _fileIds)
        {
            var descriptor = SliceDescriptor.Create(e, db.Registry);
            var datoms = db.Datoms(descriptor);
            loaded += datoms.Count;
        }

        return loaded;
    }
    
    [Benchmark]
    public int LoadAllAtOnce()
    {
        var db = _conn.Db;
        var min = _fileIds[0];
        var max = _fileIds[^1];
        var descriptor = SliceDescriptor.Create(min, max, db.Registry);
        var datoms = db.Datoms(descriptor);
        return datoms.Count;
    }
    
}
