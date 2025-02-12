using NexusMods.MnemonicDB.Storage.RocksDbBackend;

namespace NexusMods.MnemonicDB.Storage.Tests.BackendTests;

public class InMemoryTests(IServiceProvider provider)
    : ABackendTest<Backend>(provider, true);
