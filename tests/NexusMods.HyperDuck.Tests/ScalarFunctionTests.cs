using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.HyperDuck.Adaptor;
using Assert = TUnit.Assertions.Assert;

namespace NexusMods.HyperDuck.Tests;

public class ScalarFunctionTests
{
    public ScalarFunctionTests()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(s => s.AddAdapters())
            .Build();

        Services = host.Services;
        Registry = Services.GetRequiredService<IRegistry>();
    }

    public IRegistry Registry { get; set; }

    public IServiceProvider Services { get; set; }

    [Test]
    public async Task BasicScalarFunctionTest()
    {
        await using var db = DuckDB.Open(Registry);
        db.Register(new MultiplyFunction());
        var cmd = ((IQueryMixin)db).Query<int>("SELECT my_multiply(21, 2)");
        
        await Assert.That(cmd).IsEquivalentTo([42]);
    }
}

public class MultiplyFunction : AScalarFunction
{
    public override void Setup()
    {
        SetName("my_multiply");
        AddParameter<int>();
        AddParameter<int>();
        SetReturnType<int>();
    }

    public override void Execute(ReadOnlyChunk chunk, WritableVector vector)
    {
        var arg1 = chunk[0].GetData<int>();
        var arg2 = chunk[1].GetData<int>();
        var outData = vector.GetData<int>();
        for (var i = 0; i < outData.Length; i++)
        {
            outData[i] = arg1[i] * arg2[i];
        }
    }
}
