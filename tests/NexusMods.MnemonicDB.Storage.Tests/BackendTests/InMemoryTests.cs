namespace NexusMods.MnemonicDB.Storage.Tests.BackendTests;

public class InMemoryTests(IServiceProvider provider)
    : ABackendTest(provider, true);
