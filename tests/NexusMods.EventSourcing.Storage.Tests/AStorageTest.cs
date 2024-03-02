using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Serializers;

namespace NexusMods.EventSourcing.Storage.Tests;

public class AStorageTest : IAsyncLifetime
{
    protected readonly AttributeRegistry _registry;
    protected readonly NodeStore NodeStore;
    protected readonly IDatomStore DatomStore;
    private readonly InMemoryKvStore _kvStore;

    protected readonly ILogger Logger;

    public AStorageTest(IServiceProvider provider)
    {
        _registry = new AttributeRegistry(provider.GetRequiredService<IEnumerable<IValueSerializer>>(),
            provider.GetRequiredService<IEnumerable<IAttribute>>());
        _registry.Populate([
            new DbAttribute(Symbol.Intern<TestAttributes.FileHash>(), AttributeId.From(10), Symbol.Intern<UInt64Serializer>()),
            new DbAttribute(Symbol.Intern<TestAttributes.FileName>(), AttributeId.From(11), Symbol.Intern<StringSerializer>())
        ]);
        _kvStore = new InMemoryKvStore();
        NodeStore = new NodeStore(provider.GetRequiredService<ILogger<NodeStore>>(), _kvStore, _registry);
        DatomStore = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), NodeStore, _registry);
        Logger = provider.GetRequiredService<ILogger<AStorageTest>>();
    }

    public AppendableChunk TestDatomChunk(int entityCount = 100)
    {
        var chunk = new AppendableChunk();

        var emitters = new Action<EntityId, TxId, ulong>[]
        {
            (e, tx, v) => _registry.Append<TestAttributes.FileHash, ulong>(chunk, e, tx, DatomFlags.Added, v),
            (e, tx, v) => _registry.Append<TestAttributes.FileName, string>(chunk, e, tx, DatomFlags.Added, "file " + v),
        };

        for (ulong e = 0; e < (ulong)entityCount; e++)
        {
            for (var a = 0; a < 2; a ++)
            {
                for (ulong v = 0; v < 3; v++)
                {
                    emitters[a](EntityId.From(e), TxId.From(v), v);
                }
            }
        }
        return chunk;
    }

    protected static void AssertEqual(in Datom a, Datom b, int i)
    {
        a.E.Should().Be(b.E, "at index " + i);
        a.A.Should().Be(b.A, "at index " + i);
        a.T.Should().Be(b.T, "at index " + i);
        a.F.Should().Be(b.F, "at index " + i);
        a.V.Span.SequenceEqual(b.V.Span).Should().BeTrue("at index " + i);
    }

    public async Task InitializeAsync()
    {
        await DatomStore.Sync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
