namespace NexusMods.MnemonicDB.Storage.Tests.BackendTests;

[WithServiceProvider]
[InheritsTests]
public class RocksDB(IServiceProvider provider) : ABackendTest(provider, false) { }
