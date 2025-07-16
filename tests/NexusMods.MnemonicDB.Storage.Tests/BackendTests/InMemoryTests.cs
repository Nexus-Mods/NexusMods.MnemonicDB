namespace NexusMods.MnemonicDB.Storage.Tests.BackendTests;

[WithServiceProvider]
[InheritsTests]
public class InMemoryTests(IServiceProvider provider)
    : ABackendTest(provider, true);
