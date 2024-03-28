using NexusMods.MneumonicDB.Storage.InMemoryBackend;

namespace NexusMods.MneumonicDB.Storage.Tests.BackendTests;

public class InMemoryTests(IServiceProvider provider)
    : ABackendTest<Backend>(provider, registry => new Backend(registry)) { }
