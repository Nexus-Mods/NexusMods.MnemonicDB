using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Tests;

public abstract class ADatomStoreTest<TDatomStore> where TDatomStore : IDatomStore
{
    protected TDatomStore DatomStore { get; }

    protected ADatomStoreTest(TDatomStore datomStore)
    {
        DatomStore = datomStore;
    }
}
