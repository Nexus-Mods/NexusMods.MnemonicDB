using NexusMods.EventSourcing.Storage.InMemoryBackend;

namespace NexusMods.EventSourcing.Storage.Tests.BackendTests;

public class InMemoryTests(IServiceProvider provider)
    : ABackendTest<Backend>(provider, registry => new Backend(registry)) { }
