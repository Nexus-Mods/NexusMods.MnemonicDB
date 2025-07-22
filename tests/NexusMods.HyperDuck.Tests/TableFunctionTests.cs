using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.HyperDuck.Adaptor;
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
        using var db = Database.OpenInMemory(Registry);
        using var con = db.Connect();
        con.Register(new Squares());;
        var result = con.Query<List<(int, int)>>("SELECT * FROM my_squares(0, 8, stride=>1)");
        
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
        
        var result2 = con.Query<List<(int, int)>>("SELECT * FROM my_squares(0, 8000, stride=>1)");
        await Assert.That(result2).IsEquivalentTo(
            Enumerable.Range(0, 8000).Select(i => (i, i * i)));
    }

    [Test]
    public async Task CanGetQueryPlanForTable()
    {
        using var db = Database.OpenInMemory(Registry);
        using var con = db.Connect();
        con.Register(new Squares());;
        var plan = con.QueryPlan("SELECT * FROM my_squares(0, 8)");

        throw new NotImplementedException();
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
