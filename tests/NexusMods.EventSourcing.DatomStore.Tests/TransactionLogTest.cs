using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.DatomStore.Tests;

public class TransactionLogTest
{
    private readonly DatomStoreSettings _options;
    private readonly RocksDBDatomStore _db;
    private readonly IConnection _connection;


    public TransactionLogTest(IServiceProvider s)
    {
        _options = new DatomStoreSettings
        {
            Path = FileSystem.Shared.GetKnownPath(KnownPath.TempDirectory).Combine("TransactionLogTest" + Guid.NewGuid())
        };
        _db = new RocksDBDatomStore(s.GetRequiredService<ILogger<RocksDBDatomStore>>(), s.GetRequiredService<AttributeRegistry>(), _options);
        _connection = new Connection(_db);
    }

    [Fact]
    public void BasicTest()
    {


    }

    private static IDatom[] TestDatoms = [


    ];

}
