using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Tests.NodeTests;

public abstract class ADataNodeTests<TSubclass>(IServiceProvider provider)
where TSubclass : ADataNodeTests<TSubclass>
{
    protected readonly AttributeRegistry Registry =
        new(provider.GetServices<IValueSerializer>(), provider.GetServices<IAttribute>());

    protected readonly ILogger Logger = provider.GetRequiredService<ILogger<TSubclass>>();

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
