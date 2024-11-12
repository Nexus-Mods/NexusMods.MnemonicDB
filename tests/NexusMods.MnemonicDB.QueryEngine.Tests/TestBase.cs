using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.LargeTestModel;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.QueryEngine.Tests;

public class TestBase : IAsyncLifetime
{
    private readonly IConnection _connection;

    public TestBase(IConnection connection)
    {
        _connection = connection;

    }
    
    [Fact]
    public async Task CanImportLargeModlist()
    {
        Assert.True(true);
    }

    public async Task InitializeAsync()
    {
        await Importer.Import(_connection, FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("Resources/small_modlist.json.bz2"));
    }

    public async Task DisposeAsync()
    {
        
    }
}
