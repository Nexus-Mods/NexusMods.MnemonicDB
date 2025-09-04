﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.HyperDuck.Adaptor;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using Assert = TUnit.Assertions.Assert;

namespace NexusMods.HyperDuck.Tests;

public class TableFunctionTests
{
    public IRegistry Registry { get; set; }
    public IServiceProvider Services { get; set; }
    
    public TableFunctionTests()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(s => s.AddAdapters())
            .Build();

        Services = host.Services;
        Registry = Services.GetRequiredService<IRegistry>();
    }

    [Test]
    public async Task CanGetTableResults()
    {
        await using var db = DuckDB.Open(Registry);
        db.Register(new Squares());;
        var result = ((IQueryMixin)db).Query<(int, int)>("SELECT * FROM my_squares(0, 8, stride => 1)");
        
        await Assert.That(result).IsEquivalentTo([
            (0, 0), 
            (1, 1), 
            (2, 4), 
            (3, 9), 
            (4, 16), 
            (5, 25), 
            (6, 36), 
            (7, 49)
        ]);
        
        var result2 = ((IQueryMixin)db).Query<(int, int)>("SELECT * FROM my_squares(0, 8000, stride=>1)");
        await Assert.That(result2).IsEquivalentTo(
            Enumerable.Range(0, 8000).Select(i => (i, i * i)));
    }

    [Test]
    public async Task CanGetQueryPlanForTable()
    {
        await using var db = DuckDB.Open(Registry);
        var squares = new Squares();
        db.Register(squares);
        var query = ((IQueryMixin)db).Query<uint>("SELECT i FROM my_squares(0, 8)");
        var plan = db.GetReferencedFunctions(query).ToArray();

        await Assert.That(plan.Length).IsEqualTo(1);
        await Assert.That(plan[0]).IsEqualTo(squares);
    }

    [Test]
    public async Task CanCanCreateLiveQueries()
    {
        var fn = new LiveArrayOfIntsTable();
        
        await using var db = DuckDB.Open(Registry);
        db.Register(fn);

        var data = new List<int>();
        using var liveQuery = ((IQueryMixin)db).Query<int>("SELECT * FROM live_array_of_ints()").ObserveInto(data);

        await Assert.That(data).IsEmpty();
        
        fn.Ints = Enumerable.Range(0, 8).ToArray();

        await db.FlushQueries();
        
        await Assert.That(((IQueryMixin)db).Query<int>("SELECT * FROM live_array_of_ints()")).IsEquivalentTo(Enumerable.Range(0, 8));
        await Assert.That(data).IsEquivalentTo(Enumerable.Range(0, 8));

        fn.Ints = [];
        await db.FlushQueries();

        await Assert.That(data).IsEmpty();

    }

}

public class LiveArrayOfIntsTable : ATableFunction
{
    private int[] _ints = [];
    public int[] Ints
    {
        get => _ints;
        set
        {
            _ints = value;
            Revise();
        }
    }

    protected override void Setup(RegistrationInfo info)
    {
        info.SetName("live_array_of_ints");
    }

    protected override void Bind(BindInfo info)
    {
        info.AddColumn<int>("ints");
    }

    protected override object? Init(InitInfo initInfo, InitData initData)
    {
        return new ScanState
        {
            Index = 0,
            Ints = Ints,
        };
    }

    protected override void Execute(FunctionInfo functionInfo)
    {
        var chunk = functionInfo.Chunk;
        var initData = functionInfo.GetInitInfo<ScanState>();
        var vecA = chunk[0].GetData<int>();
        
        var fromIdx = initData.Index;
        var length = Math.Min(vecA.Length, initData.Ints.Length - initData.Index);
        initData.Ints.AsSpan().Slice(fromIdx, length).CopyTo(vecA);
        chunk.Size = (ulong)length;
        initData.Index += length;
    }

    private class ScanState
    {
        public required int Index { get; set; }
        public required int[] Ints { get; init; }
    }

}

public class Squares : ATableFunction
{
    protected override void Setup(RegistrationInfo info)
    {
        info.SetName("my_squares");
        info.AddParameter<int>();
        info.AddParameter<int>();
        info.AddNamedParameter<int>("stride");
    }

    protected override void Execute(FunctionInfo functionInfo)
    {
        var chunk = functionInfo.Chunk;
        var bindInfo = functionInfo.GetBindInfo<State>();
        var vecA = chunk[0].GetData<int>();
        var vecB = chunk[1].GetData<int>();

        var row = 0;
        for (var i = bindInfo.Start; i < bindInfo.End && row < functionInfo.EmitSize ; i += bindInfo.Stride)
        {
            vecA[row] = i;
            vecB[row] = i * i;
            row++;
        }
        
        chunk.Size = (ulong)row;
        bindInfo.Start += (row * bindInfo.Stride);
        
    }

    protected override void Bind(BindInfo info)
    {
        // Tell DuckDB about what columns we will emit. Notice that this is happening during the "bind" phase,
        // meaning we can emit different columns (and column types) based on the input parameters
        
        info.AddColumn<int>("i");
        info.AddColumn<int>("o");
        using var start = info.GetParameter(0);
        using var end = info.GetParameter(1);
        using var stride = info.GetParameter("stride");
        var state = new State
        {
            Start = start.GetInt32(),
            End = end.GetInt32(),
            Stride = stride.GetDefault(1),
        };
        
        info.SetBindInfo(state);
    }

    private class State
    {
        public int Start;
        public int End;
        public int Stride;
    }
}
