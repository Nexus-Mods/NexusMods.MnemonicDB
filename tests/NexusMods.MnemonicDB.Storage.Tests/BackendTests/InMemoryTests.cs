using NexusMods.MnemonicDB.Storage.InMemoryBackend;

namespace NexusMods.MnemonicDB.Storage.Tests.BackendTests;

public class InMemoryTests(IServiceProvider provider)
    : ABackendTest<Backend>(provider, registry => new Backend(registry)) { }
