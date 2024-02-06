using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Tests;


public class DataModelTests
{
    private readonly DuckDBDatomStore _datastore;
    private readonly IEntityRegistry _entityRegistry;
    private readonly IConnection _connection;

    public DataModelTests(IServiceProvider serviceProvider, IEntityRegistry entityRegistry)
    {
        _entityRegistry = entityRegistry;
        _datastore = new DuckDBDatomStore(serviceProvider.GetRequiredService<ILogger<DuckDBDatomStore>>());
        _connection = new Connection(_datastore, _entityRegistry);

    }
    [Fact]
    public void CanInsertMods()
    {
        using var tx = _connection.BeginTransaction();
        var mod = new Mod(tx)
        {
            Name = "Test Mod",
            Description = "This is a test mod",
        };

        var result = tx.Commit();

        var refreshedMod = result.Refresh(mod);

        refreshedMod.Id.Should().NotBe(mod.Id);
        refreshedMod.Name.Should().Be(mod.Name);
        refreshedMod.Description.Should().Be(mod.Description);





    }
}
