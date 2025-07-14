using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        Builder = Services.GetRequiredService<Builder>();
    }

    public Builder Builder { get; set; }

    public IServiceProvider Services { get; set; }

    [Test]
    public async Task CanGetScalarResults()
    {
        using var db = Database.OpenInMemory();
        using var con = db.Connect();
        var result = con.Query<List<int>>("SELECT 1 AS one", Builder);

        await Assert.That(result).IsEquivalentTo([1]);

        var result2 = con.Query<List<long>>("SELECT * FROM generate_series(1, 10, 1)", Builder);

        await Assert.That(result2).IsEquivalentTo(Enumerable.Range(1, 10).Select(s => (long)s));
    }

    [Test]
    public async Task CanGetTupleResults()
    {
        using var db = Database.OpenInMemory();
        using var con = db.Connect();
        var result = con.Query<List<(int, int)>>("SELECT 1 AS one, 2 AS two", Builder);

        await Assert.That(result).IsEquivalentTo([(1, 2)]);
    }

    [Test]
    public async Task CanGetStringResults()
    {
        using var db = Database.OpenInMemory();
        using var con = db.Connect();
        var result =
            con.Query<List<(string, string)>>(
                "SELECT 'Hello' AS one, 'A really long string that cannot be inlined' AS two", Builder);

        await Assert.That(result).IsEquivalentTo([("Hello", "A really long string that cannot be inlined")]);
    }
    
    [Test]
    public async Task CanGetListResults()
    {
        using var db = Database.OpenInMemory();
        using var con = db.Connect();
        var result = con.Query<List<(List<int>, int)>>("SELECT [1, 2, 3] as lst, 42 as i", Builder);

        await Assert.That(result).IsEquivalentTo([(new List<int> { 1, 2, 3 }, 42)]);

    }

}
