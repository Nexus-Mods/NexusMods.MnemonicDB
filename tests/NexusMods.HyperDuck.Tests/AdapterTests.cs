using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using NexusMods.HyperDuck.Adaptor;
using Assert = TUnit.Assertions.Assert;

namespace NexusMods.HyperDuck.Tests;

public class AdapterTests
{
    public AdapterTests()
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
    public async Task CanGetScalarResults()
    {
        using var db = Database.OpenInMemory(Registry);
        var queryOne = Query.Compile<List<int>>("SELECT 1 AS one");
        var result = db.Query(queryOne);

        await Assert.That(result).IsEquivalentTo([1]);

        var querySeries = Query.Compile<List<long>>("SELECT * FROM generate_series(1, 10, 1)");
        var result2 = db.Query(querySeries);

        await Assert.That(result2).IsEquivalentTo(Enumerable.Range(1, 10).Select(s => (long)s));
    }

    [Test]
    public async Task CanGetTupleResults()
    {
        using var db = Database.OpenInMemory(Registry);
        var queryTwoColumns = Query.Compile<List<(int, int)>>("SELECT 1 AS one, 2 AS two");
        var result = db.Query(queryTwoColumns);

        await Assert.That(result).IsEquivalentTo([(1, 2)]);
    }

    [Test]
    public async Task CanGetStringResults()
    {
        using var db = Database.OpenInMemory(Registry);
        var queryTwoStrings = Query.Compile<List<(string, string)>>("SELECT 'Hello' AS one, 'A really long string that cannot be inlined' AS two");
        var result = db.Query(queryTwoStrings);

        await Assert.That(result).IsEquivalentTo([("Hello", "A really long string that cannot be inlined")]);
    }
    
    [Test]
    public async Task CanGetListResults()
    {
        using var db = Database.OpenInMemory(Registry);
        var queryList = Query.Compile<List<(List<int>, int)>>("SELECT [1, 2, 3] as lst, 42 as i");
        var result = db.Query(queryList);

        await Assert.That(result).IsEquivalentTo([(new List<int> { 1, 2, 3 }, 42)]);

    }

}
