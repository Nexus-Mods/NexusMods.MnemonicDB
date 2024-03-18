using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Nodes.Data;
using NexusMods.EventSourcing.Storage.Serializers;

namespace NexusMods.EventSourcing.Storage.Tests.NodeTests;

public abstract class ADataNodeTests<TSubclass> where TSubclass : ADataNodeTests<TSubclass>
{
    protected readonly AttributeRegistry Registry;

    protected readonly ILogger Logger;
    protected readonly NodeStore NodeStore;
    private readonly InMemoryKvStore _kvStore;

    protected ADataNodeTests(IServiceProvider provider)
    {
        Registry = new(provider.GetServices<IValueSerializer>(), provider.GetServices<IAttribute>());
        Logger = provider.GetRequiredService<ILogger<TSubclass>>();
        Registry.Populate([
            new DbAttribute(Symbol.Intern("test/attr1"), AttributeId.From(10), new StringSerializer().UniqueId)
        ]);

        _kvStore = new InMemoryKvStore();
        NodeStore = new NodeStore(_kvStore, Registry);
    }

    #region Helpers

    protected IEnumerable<Datom> TestData(uint max)
    {
        for (ulong eid = 0; eid < max; eid += 1)
        {
            for (ulong tx = 0; tx < 10; tx += 1)
            {
                for (ulong val = 1; val < 10; val += 1)
                {
                    yield return new Datom()
                    {
                        E = EntityId.From(eid),
                        A = AttributeId.From(10),
                        T = TxId.From(tx),
                        V = BitConverter.GetBytes(val)
                    };
                }
            }
        }
    }

    protected IComparer<Datom> CreateComparer(IDatomComparator datomComparator)
    {
        return Comparer<Datom>.Create((a, b) => datomComparator.Compare(in a, in b));
    }

    #endregion

}
