using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.Tests;

public abstract class ABackendTest<TStoreType>(IServiceProvider provider, Func<AttributeRegistry, IStoreBackend> backendFn)
    : AStorageTest(provider, backendFn)
    where TStoreType : IStoreBackend
{
}
